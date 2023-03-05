﻿namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Geometry;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Core;
    using UglyToad.PdfPig.Graphics.Operations;
    using UglyToad.PdfPig.Graphics.Operations.TextPositioning;
    using UglyToad.PdfPig.Parser;
    using UglyToad.PdfPig.PdfFonts;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.XObjects;

    /// <summary>
    /// TODO
    /// </summary>
    public abstract class BaseStreamProcessor<T> : IOperationContext
    {
        internal readonly IResourceStore resourceStore;
        internal readonly UserSpaceUnit userSpaceUnit;
        internal readonly PageRotationDegrees rotation;
        internal readonly IPdfTokenScanner pdfScanner;
        internal readonly IPageContentParser pageContentParser;
        internal readonly ILookupFilterProvider filterProvider;
        internal readonly PdfVector pageSize;
        internal readonly InternalParsingOptions parsingOptions;

        internal Stack<CurrentGraphicsState> graphicsStack = new Stack<CurrentGraphicsState>();
        internal IFont activeExtendedGraphicsStateFont;
        internal InlineImageBuilder inlineImageBuilder;
        internal int pageNumber;

        /// <summary>
        /// A counter to track individual calls to <see cref="ShowText"/> operations used to determine if letters are likely to be
        /// in the same word/group. This exposes internal grouping of letters used by the PDF creator which may correspond to the
        /// intended grouping of letters into words.
        /// </summary>
        protected int TextSequence;

        /// <summary>
        /// TODO
        /// </summary>
        public TextMatrices TextMatrices { get; } = new TextMatrices();

        /// <summary>
        /// TODO
        /// </summary>
        public TransformationMatrix CurrentTransformationMatrix => GetCurrentState().CurrentTransformationMatrix;

        /// <summary>
        /// TODO
        /// </summary>
        public PdfPoint CurrentPosition { get; set; }

        /// <summary>
        /// TODO
        /// </summary>
        public int StackSize => graphicsStack.Count;

        internal readonly Dictionary<XObjectType, List<XObjectContentRecord>> xObjects = new Dictionary<XObjectType, List<XObjectContentRecord>>
        {
            {XObjectType.Image, new List<XObjectContentRecord>()},
            {XObjectType.PostScript, new List<XObjectContentRecord>()}
        };

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="cropBox"></param>
        /// <param name="resourceStore"></param>
        /// <param name="userSpaceUnit"></param>
        /// <param name="rotation"></param>
        /// <param name="pdfScanner"></param>
        /// <param name="pageContentParser"></param>
        /// <param name="filterProvider"></param>
        /// <param name="pageSize"></param>
        /// <param name="parsingOptions"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal BaseStreamProcessor(PdfRectangle cropBox, IResourceStore resourceStore, UserSpaceUnit userSpaceUnit, PageRotationDegrees rotation,
            IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser,
            ILookupFilterProvider filterProvider,
            PdfVector pageSize,
            InternalParsingOptions parsingOptions)
        {
            this.resourceStore = resourceStore;
            this.userSpaceUnit = userSpaceUnit;
            this.rotation = rotation;
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.pageContentParser = pageContentParser ?? throw new ArgumentNullException(nameof(pageContentParser));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.pageSize = pageSize;
            this.parsingOptions = parsingOptions;

            // initiate CurrentClippingPath to cropBox
            var clippingSubpath = new PdfSubpath();
            clippingSubpath.Rectangle(cropBox.BottomLeft.X, cropBox.BottomLeft.Y, cropBox.Width, cropBox.Height);
            var clippingPath = new PdfPath() { clippingSubpath };
            clippingPath.SetClipping(FillingRule.EvenOdd);

            graphicsStack.Push(new CurrentGraphicsState()
            {
                CurrentTransformationMatrix = GetInitialMatrix(),
                CurrentClippingPath = clippingPath
            });

            GetCurrentState().ColorSpaceContext = new ColorSpaceContext(GetCurrentState, resourceStore);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [System.Diagnostics.Contracts.Pure]
        protected TransformationMatrix GetInitialMatrix()
        {
            /* 
             * There should be a single Affine Transform we can apply to any point resulting
             * from a content stream operation which will rotate the point and translate it back to
             * a point where the origin is in the page's lower left corner.
             *
             * For example this matrix represents a (clockwise) rotation and translation:
             * [  cos  sin  tx ]
             * [ -sin  cos  ty ]
             * [    0    0   1 ]
             * Warning: rotation is counter-clockwise here
             * 
             * The values of tx and ty are those required to move the origin back to the expected origin (lower-left).
             * The corresponding values should be:
             * Rotation:  0   90  180  270
             *       tx:  0    0    w    w
             *       ty:  0    h    h    0
             *
             * Where w and h are the page width and height after rotation.
            */

            double cos, sin;
            double dx = 0, dy = 0;
            switch (rotation.Value)
            {
                case 0:
                    cos = 1;
                    sin = 0;
                    break;
                case 90:
                    cos = 0;
                    sin = 1;
                    dy = pageSize.Y;
                    break;
                case 180:
                    cos = -1;
                    sin = 0;
                    dx = pageSize.X;
                    dy = pageSize.Y;
                    break;
                case 270:
                    cos = 0;
                    sin = -1;
                    dx = pageSize.X;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid value for page rotation: {rotation.Value}.");
            }

            return new TransformationMatrix(
                cos, -sin, 0,
                sin, cos, 0,
                dx, dy, 1);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="pageNumberCurrent"></param>
        /// <param name="operations"></param>
        /// <returns></returns>
        public abstract T Process(int pageNumberCurrent, IReadOnlyList<IGraphicsStateOperation> operations);

        /// <summary>
        /// TODO
        /// </summary>
        protected void ProcessOperations(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            foreach (var stateOperation in operations)
            {
                stateOperation.Run(this);
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        protected Stack<CurrentGraphicsState> CloneAllStates()
        {
            var saved = graphicsStack;
            graphicsStack = new Stack<CurrentGraphicsState>();
            graphicsStack.Push(saved.Peek().DeepClone());
            return saved;
        }

        /// <summary>
        /// TODO
        /// </summary>
        [DebuggerStepThrough]
        public CurrentGraphicsState GetCurrentState()
        {
            return graphicsStack.Peek();
        }

        /// <inheritdoc/>
        public virtual void PopState()
        {
            graphicsStack.Pop();
            activeExtendedGraphicsStateFont = null;
        }

        /// <inheritdoc/>
        public virtual void PushState()
        {
            graphicsStack.Push(graphicsStack.Peek().DeepClone());
        }

        /// <inheritdoc/>
        public void ShowText(IInputBytes bytes)
        {
            var currentState = GetCurrentState();

            var font = currentState.FontState.FromExtendedGraphicsState ? activeExtendedGraphicsStateFont : resourceStore.GetFont(currentState.FontState.FontName);

            if (font == null)
            {
                if (parsingOptions.SkipMissingFonts)
                {
                    parsingOptions.Logger.Warn($"Skipping a missing font with name {currentState.FontState.FontName} " +
                                               $"since it is not present in the document and {nameof(InternalParsingOptions.SkipMissingFonts)} " +
                                               "is set to true. This may result in some text being skipped and not included in the output.");

                    return;
                }

                throw new InvalidOperationException($"Could not find the font with name {currentState.FontState.FontName} in the resource store. It has not been loaded yet.");
            }

            var fontSize = currentState.FontState.FontSize;
            var horizontalScaling = currentState.FontState.HorizontalScaling / 100.0;
            var characterSpacing = currentState.FontState.CharacterSpacing;
            var rise = currentState.FontState.Rise;

            var transformationMatrix = currentState.CurrentTransformationMatrix;

            var renderingMatrix =
                TransformationMatrix.FromValues(fontSize * horizontalScaling, 0, 0, fontSize, 0, rise);

            var pointSize = Math.Round(transformationMatrix.Multiply(TextMatrices.TextMatrix).Transform(new PdfRectangle(0, 0, 1, fontSize)).Height, 2);

            while (bytes.MoveNext())
            {
                var code = font.ReadCharacterCode(bytes, out int codeLength);

                var foundUnicode = font.TryGetUnicode(code, out var unicode);

                if (!foundUnicode || unicode == null)
                {
                    parsingOptions.Logger.Warn($"We could not find the corresponding character with code {code} in font {font.Name}.");

                    // Try casting directly to string as in PDFBox 1.8.
                    unicode = new string((char)code, 1);
                }

                var wordSpacing = 0.0;
                if (code == ' ' && codeLength == 1)
                {
                    wordSpacing += GetCurrentState().FontState.WordSpacing;
                }

                var textMatrix = TextMatrices.TextMatrix;

                if (font.IsVertical)
                {
                    if (!(font is IVerticalWritingSupported verticalFont))
                    {
                        throw new InvalidOperationException($"Font {font.Name} was in vertical writing mode but did not implement {nameof(IVerticalWritingSupported)}.");
                    }

                    var positionVector = verticalFont.GetPositionVector(code);

                    textMatrix = textMatrix.Translate(positionVector.X, positionVector.Y);
                }

                var boundingBox = font.GetBoundingBox(code);

                // If the text rendering mode calls for filling, the current nonstroking color in the graphics state is used; 
                // if it calls for stroking, the current stroking color is used.
                // In modes that perform both filling and stroking, the effect is as if each glyph outline were filled and then stroked in separate operations.
                // TODO: expose color as something more advanced
                var color = currentState.FontState.TextRenderingMode != TextRenderingMode.Stroke
                    ? currentState.CurrentNonStrokingColor
                    : currentState.CurrentStrokingColor;

                RenderGlyph(font, color, fontSize, pointSize, code, unicode, bytes.CurrentOffset, renderingMatrix, textMatrix, transformationMatrix, boundingBox);

                double tx, ty;
                if (font.IsVertical)
                {
                    var verticalFont = (IVerticalWritingSupported)font;
                    var displacement = verticalFont.GetDisplacementVector(code);
                    tx = 0;
                    ty = (displacement.Y * fontSize) + characterSpacing + wordSpacing;
                }
                else
                {
                    tx = (boundingBox.Width * fontSize + characterSpacing + wordSpacing) * horizontalScaling;
                    ty = 0;
                }

                TextMatrices.TextMatrix = TextMatrices.TextMatrix.Translate(tx, ty);
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public abstract void RenderGlyph(IFont font, IColor color, double fontSize, double pointSize, int code, string unicode, long currentOffset,
            TransformationMatrix renderingMatrix, TransformationMatrix textMatrix, TransformationMatrix transformationMatrix, CharacterBoundingBox characterBoundingBox);

        /// <inheritdoc/>
        public void ShowPositionedText(IReadOnlyList<IToken> tokens)
        {
            TextSequence++;

            var currentState = GetCurrentState();

            var textState = currentState.FontState;

            var fontSize = textState.FontSize;
            var horizontalScaling = textState.HorizontalScaling / 100.0;
            var font = resourceStore.GetFont(textState.FontName);

            var isVertical = font.IsVertical;

            foreach (var token in tokens)
            {
                if (token is NumericToken number)
                {
                    var positionAdjustment = (double)number.Data;

                    double tx, ty;
                    if (isVertical)
                    {
                        tx = 0;
                        ty = -positionAdjustment / 1000 * fontSize;
                    }
                    else
                    {
                        tx = -positionAdjustment / 1000 * fontSize * horizontalScaling;
                        ty = 0;
                    }

                    AdjustTextMatrix(tx, ty);
                }
                else
                {
                    IReadOnlyList<byte> bytes;
                    if (token is HexToken hex)
                    {
                        bytes = hex.Bytes;
                    }
                    else
                    {
                        bytes = OtherEncodings.StringAsLatin1Bytes(((StringToken)token).Data);
                    }

                    ShowText(new ByteArrayInputBytes(bytes));
                }
            }
        }

        /// <inheritdoc/>
        public void ApplyXObject(NameToken xObjectName)
        {
            var xObjectStream = resourceStore.GetXObject(xObjectName);

            // For now we will determine the type and store the object with the graphics state information preceding it.
            // Then consumers of the page can request the object(s) to be retrieved by type.
            var subType = (NameToken)xObjectStream.StreamDictionary.Data[NameToken.Subtype.Data];

            var state = GetCurrentState();

            var matrix = state.CurrentTransformationMatrix;

            if (subType.Equals(NameToken.Ps))
            {
                var contentRecord = new XObjectContentRecord(XObjectType.PostScript, xObjectStream, matrix, state.RenderingIntent,
                    state.CurrentStrokingColor?.ColorSpace ?? ColorSpace.DeviceRGB);

                xObjects[XObjectType.PostScript].Add(contentRecord);
            }
            else if (subType.Equals(NameToken.Image))
            {
                var contentRecord = new XObjectContentRecord(XObjectType.Image, xObjectStream, matrix, state.RenderingIntent,
                    state.ColorSpaceContext?.CurrentStrokingColorSpaceDetails?.Type ?? ColorSpace.DeviceRGB);

                RenderXObjectImage(contentRecord);
            }
            else if (subType.Equals(NameToken.Form))
            {
                ProcessFormXObject(xObjectStream);
            }
            else
            {
                throw new InvalidOperationException($"XObject encountered with unexpected SubType {subType}. {xObjectStream.StreamDictionary}.");
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="xObjectContentRecord"></param>
        public abstract void RenderXObjectImage(XObjectContentRecord xObjectContentRecord);

        /// <summary>
        /// TODO
        /// </summary>
        protected void ProcessFormXObject(StreamToken formStream)
        {
            /*
             * When a form XObject is invoked the following should happen:
             *
             * 1. Save the current graphics state, as if by invoking the q operator.
             * 2. Concatenate the matrix from the form dictionary's Matrix entry with the current transformation matrix.
             * 3. Clip according to the form dictionary's BBox entry.
             * 4. Paint the graphics objects specified in the form's content stream.
             * 5. Restore the saved graphics state, as if by invoking the Q operator.
             */

            var hasResources = formStream.StreamDictionary.TryGet<DictionaryToken>(NameToken.Resources, pdfScanner, out var formResources);
            if (hasResources)
            {
                resourceStore.LoadResourceDictionary(formResources, parsingOptions);
            }

            // 1. Save current state.
            PushState();

            var startState = GetCurrentState();

            var formMatrix = TransformationMatrix.Identity;
            if (formStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Matrix, pdfScanner, out var formMatrixToken))
            {
                formMatrix = TransformationMatrix.FromArray(formMatrixToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray());
            }

            // 2. Update current transformation matrix.
            startState.CurrentTransformationMatrix = formMatrix.Multiply(startState.CurrentTransformationMatrix);

            var contentStream = formStream.Decode(filterProvider, pdfScanner);

            var operations = pageContentParser.Parse(pageNumber, new ByteArrayInputBytes(contentStream), parsingOptions.Logger);

            // 3. We don't respect clipping currently.

            // 4. Paint the objects.
            ProcessOperations(operations);

            // 5. Restore saved state.
            PopState();

            if (hasResources)
            {
                resourceStore.UnloadResourceDictionary();
            }
        }

        /// <inheritdoc/>
        public abstract void BeginSubpath();

        /// <inheritdoc/>
        public abstract PdfPoint? CloseSubpath();

        /// <inheritdoc/>
        public abstract void StrokePath(bool close);

        /// <inheritdoc/>
        public abstract void FillPath(FillingRule fillingRule, bool close);

        /// <inheritdoc/>
        public abstract void FillStrokePath(FillingRule fillingRule, bool close);

        /// <inheritdoc/>
        public abstract void MoveTo(double x, double y);

        /// <inheritdoc/>
        public abstract void BezierCurveTo(double x2, double y2, double x3, double y3);

        /// <inheritdoc/>
        public abstract void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3);

        /// <inheritdoc/>
        public abstract void LineTo(double x, double y);

        /// <inheritdoc/>
        public abstract void Rectangle(double x, double y, double width, double height);

        /// <inheritdoc/>
        public abstract void EndPath();

        /// <inheritdoc/>
        public abstract void ClosePath();

        /// <inheritdoc/>
        public abstract void ModifyClippingIntersect(FillingRule clippingRule);

        /// <inheritdoc/>
        public void SetNamedGraphicsState(NameToken stateName)
        {
            var currentGraphicsState = GetCurrentState();

            var state = resourceStore.GetExtendedGraphicsStateDictionary(stateName);

            if (state.TryGet(NameToken.Lw, pdfScanner, out NumericToken lwToken))
            {
                currentGraphicsState.LineWidth = lwToken.Data;
            }

            if (state.TryGet(NameToken.Lc, pdfScanner, out NumericToken lcToken))
            {
                currentGraphicsState.CapStyle = (LineCapStyle)lcToken.Int;
            }

            if (state.TryGet(NameToken.Lj, pdfScanner, out NumericToken ljToken))
            {
                currentGraphicsState.JoinStyle = (LineJoinStyle)ljToken.Int;
            }

            if (state.TryGet(NameToken.Font, pdfScanner, out ArrayToken fontArray) && fontArray.Length == 2
                && fontArray.Data[0] is IndirectReferenceToken fontReference && fontArray.Data[1] is NumericToken sizeToken)
            {
                currentGraphicsState.FontState.FromExtendedGraphicsState = true;
                currentGraphicsState.FontState.FontSize = (double)sizeToken.Data;
                activeExtendedGraphicsStateFont = resourceStore.GetFontDirectly(fontReference);
            }

            if (state.TryGet(NameToken.Ais, pdfScanner, out BooleanToken aisToken))
            {
                // Page 223
                // The alpha source flag (“alpha is shape”), specifying
                // whether the current soft mask and alpha constant are to be interpreted as
                // shape values (true) or opacity values (false).
                currentGraphicsState.AlphaSource = aisToken.Data;
            }

            if (state.TryGet(NameToken.Bm, pdfScanner, out NameToken bmNameToken))
            {
                // Page 223
                // (Optional; PDF 1.4) The current blend mode to be used in the transparent
                // imaging model (see Sections 7.2.4, “Blend Mode,” and 7.5.2, “Specifying
                // Blending Color Space and Blend Mode”).
                SetBlendModeFromToken(bmNameToken);
            }

            if (state.TryGet(NameToken.Bm, pdfScanner, out ArrayToken bmArrayToken))
            {
                // Page 223
                // (Optional; PDF 1.4) The current blend mode to be used in the transparent
                // imaging model (see Sections 7.2.4, “Blend Mode,” and 7.5.2, “Specifying
                // Blending Color Space and Blend Mode”).
                foreach (var item in bmArrayToken.Data)
                {
                    SetBlendModeFromToken(bmNameToken); // TODO - why for loop??
                }
            }

            if (state.TryGet(NameToken.Ca, pdfScanner, out NumericToken caToken))
            {
                // Page 223
                // (Optional; PDF 1.4) The current stroking alpha constant, specifying the constant shape or constant opacity value to be used for stroking operations in the
                // transparent imaging model (see “Source Shape and Opacity” on page 526 and
                // “Constant Shape and Opacity” on page 551).
                currentGraphicsState.AlphaConstantStroking = caToken.Data;
            }

            if (state.TryGet(NameToken.CaNs, pdfScanner, out NumericToken cansToken))
            {
                // Page 223
                // (Optional; PDF 1.4) The current stroking alpha constant, specifying the constant shape or constant opacity value to be used for NON-stroking operations in the
                // transparent imaging model (see “Source Shape and Opacity” on page 526 and
                // “Constant Shape and Opacity” on page 551).
                currentGraphicsState.AlphaConstantNonStroking = cansToken.Data;
            }

            if (state.TryGet(NameToken.Op, pdfScanner, out BooleanToken OPToken))
            {
                // Page 223
                // (Optional) A flag specifying whether to apply overprint (see Section 4.5.6,
                // “Overprint Control”). In PDF 1.2 and earlier, there is a single overprint
                // parameter that applies to all painting operations. Beginning with PDF 1.3,
                // there are two separate overprint parameters: one for stroking and one for all
                // other painting operations. Specifying an OP entry sets both parameters unless there is also an op entry in the same graphics state parameter dictionary,
                // in which case the OP entry sets only the overprint parameter for stroking.
                currentGraphicsState.Overprint = OPToken.Data;
            }

            if (state.TryGet(NameToken.OpNs, pdfScanner, out BooleanToken opToken))
            {
                // Page 223
                // (Optional; PDF 1.3) A flag specifying whether to apply overprint (see Section
                // 4.5.6, “Overprint Control”) for painting operations other than stroking. If
                // this entry is absent, the OP entry, if any, sets this parameter.
                //
                // Page 284
                currentGraphicsState.NonStrokingOverprint = opToken.Data;
            }

            if (state.TryGet(NameToken.Opm, pdfScanner, out NumericToken opmToken))
            {
                // Page 223
                // (Optional; PDF 1.3) The overprint mode (see Section 4.5.6, “Overprint Control”).
                //
                // Page 284
                currentGraphicsState.OverprintMode = opmToken.Data;
            }

            if (state.TryGet(NameToken.Sa, pdfScanner, out BooleanToken saToken))
            {
                // Page 223
                // (Optional) A flag specifying whether to apply automatic stroke adjustment
                // (see Section 6.5.4, “Automatic Stroke Adjustment”).
                currentGraphicsState.StrokeAdjustment = saToken.Data;
            }

            if (state.TryGet(NameToken.Smask, pdfScanner, out NameToken smaskToken))
            {
                // Page 223
                // (Optional; PDF 1.4) The current soft mask, specifying the mask shape or
                // mask opacity values to be used in the transparent imaging model (see
                // “Source Shape and Opacity” on page 526 and “Mask Shape and Opacity” on
                // page 550).
                if (smaskToken.Data == NameToken.None.Data)
                {
                    // TODO: Replace soft mask with nothing.
                }
            }
        }

        private void SetBlendModeFromToken(NameToken bmNameToken)
        {
            // Standard separable blend modes -  1.7 - Page 520
            if (bmNameToken.Data == NameToken.Normal)
            {
                // TODO
            }
            else if (bmNameToken.Data == NameToken.Multiply)
            {
                // TODO
            }
            else if (bmNameToken.Data == NameToken.Screen)
            {
                // TODO
            }
            else if (bmNameToken.Data == NameToken.Overlay)
            {
                // TODO
            }
            else if (bmNameToken.Data == NameToken.Darken)
            {
                // TODO
            }
            else if (bmNameToken.Data == NameToken.Lighten)
            {
                // TODO
            }
            else if (bmNameToken.Data == NameToken.ColorDodge)
            {
                // TODO
            }
            else if (bmNameToken.Data == NameToken.ColorBurn)
            {
                // TODO
            }
            else if (bmNameToken.Data == NameToken.HardLight)
            {
                // TODO
            }
            else if (bmNameToken.Data == NameToken.SoftLight)
            {
                // TODO
            }
            else if (bmNameToken.Data == NameToken.Difference)
            {
                // TODO
            }
            else if (bmNameToken.Data == NameToken.Exclusion)
            {
                // TODO
            }

            // Standard nonseparable blend modes - Page 524
            if (bmNameToken.Data == NameToken.Normal)
            {
                // TODO
            }
            else if (bmNameToken.Data == "Hue")
            {
                // TODO
            }
            else if (bmNameToken.Data == "Saturation")
            {
                // TODO
            }
            else if (bmNameToken.Data == "Color")
            {
                // TODO
            }
            else if (bmNameToken.Data == "Luminosity")
            {
                // TODO
            }
        }

        /// <inheritdoc/>
        public void BeginInlineImage()
        {
            if (inlineImageBuilder != null)
            {
                parsingOptions.Logger.Error("Begin inline image (BI) command encountered while another inline image was active.");
            }

            inlineImageBuilder = new InlineImageBuilder();
        }

        /// <inheritdoc/>
        public void SetInlineImageProperties(IReadOnlyDictionary<NameToken, IToken> properties)
        {
            if (inlineImageBuilder == null)
            {
                parsingOptions.Logger.Error("Begin inline image data (ID) command encountered without a corresponding begin inline image (BI) command.");
                return;
            }

            inlineImageBuilder.Properties = properties;
        }

        /// <inheritdoc/>
        public void EndInlineImage(IReadOnlyList<byte> bytes)
        {
            if (inlineImageBuilder == null)
            {
                parsingOptions.Logger.Error("End inline image (EI) command encountered without a corresponding begin inline image (BI) command.");
                return;
            }

            inlineImageBuilder.Bytes = bytes;

            var image = inlineImageBuilder.CreateInlineImage(CurrentTransformationMatrix, filterProvider, pdfScanner, GetCurrentState().RenderingIntent, resourceStore);

            RenderInlineImage(image);

            inlineImageBuilder = null;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="inlineImage"></param>
        public abstract void RenderInlineImage(InlineImage inlineImage);

        /// <inheritdoc/>
        public abstract void BeginMarkedContent(NameToken name, NameToken propertyDictionaryName, DictionaryToken properties);

        /// <inheritdoc/>
        public abstract void EndMarkedContent();

        /// <inheritdoc/>
        public abstract void ApplyShading(NameToken shading);

        /// <summary>
        /// TODO
        /// </summary>
        protected void AdjustTextMatrix(double tx, double ty)
        {
            var matrix = TransformationMatrix.GetTranslationMatrix(tx, ty);
            TextMatrices.TextMatrix = matrix.Multiply(TextMatrices.TextMatrix);
        }

        /// <inheritdoc/>
        public void SetFlatnessTolerance(decimal tolerance)
        {
            GetCurrentState().Flatness = tolerance;
        }

        /// <inheritdoc/>
        public void SetLineCap(LineCapStyle cap)
        {
            GetCurrentState().CapStyle = cap;
        }

        /// <inheritdoc/>
        public void SetLineDashPattern(LineDashPattern pattern)
        {
            GetCurrentState().LineDashPattern = pattern;
        }

        /// <inheritdoc/>
        public void SetLineJoin(LineJoinStyle join)
        {
            GetCurrentState().JoinStyle = join;
        }

        /// <inheritdoc/>
        public void SetLineWidth(decimal width)
        {
            GetCurrentState().LineWidth = width;
        }

        /// <inheritdoc/>
        public void SetMiterLimit(decimal limit)
        {
            GetCurrentState().MiterLimit = limit;
        }

        /// <inheritdoc/>
        public void MoveToNextLineWithOffset()
        {
            var tdOperation = new MoveToNextLineWithOffset(0, -1 * (decimal)GetCurrentState().FontState.Leading);
            tdOperation.Run(this);
        }

        /// <inheritdoc/>
        public void SetFontAndSize(NameToken font, double size)
        {
            var currentState = GetCurrentState();
            currentState.FontState.FontSize = size;
            currentState.FontState.FontName = font;
        }

        /// <inheritdoc/>
        public void SetHorizontalScaling(double scale)
        {
            GetCurrentState().FontState.HorizontalScaling = scale;
        }

        /// <inheritdoc/>
        public void SetTextLeading(double leading)
        {
            GetCurrentState().FontState.Leading = leading;
        }

        /// <inheritdoc/>
        public void SetTextRenderingMode(TextRenderingMode mode)
        {
            GetCurrentState().FontState.TextRenderingMode = mode;
        }

        /// <inheritdoc/>
        public void SetTextRise(double rise)
        {
            GetCurrentState().FontState.Rise = rise;
        }

        /// <inheritdoc/>
        public void SetWordSpacing(double spacing)
        {
            GetCurrentState().FontState.WordSpacing = spacing;
        }

        /// <inheritdoc/>
        public void ModifyCurrentTransformationMatrix(double[] value)
        {
            var ctm = GetCurrentState().CurrentTransformationMatrix;
            GetCurrentState().CurrentTransformationMatrix = TransformationMatrix.FromArray(value).Multiply(ctm);
        }

        /// <inheritdoc/>
        public void SetCharacterSpacing(double spacing)
        {
            GetCurrentState().FontState.CharacterSpacing = spacing;
        }
    }
}
