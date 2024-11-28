namespace UglyToad.PdfPig.PdfFonts.Simple
{    
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Cmap;
    using Composite;
    using Core;
    using Fonts;
    using Fonts.Encodings;
    using Graphics;
    using Graphics.Operations.TextState;
    using Tokens;
    using UglyToad.PdfPig.Graphics.Operations;

    internal class Type3Font : IType3Font
    {
        private readonly PdfRectangle boundingBox;
        private readonly TransformationMatrix fontMatrix;
        private readonly Encoding encoding;
        private readonly int firstChar;
        private readonly int lastChar;
        private readonly double[] widths;
        private readonly ToUnicodeCMap toUnicodeCMap;
        private readonly bool isZapfDingbats;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<IGraphicsStateOperation>> charProcs;

        /// <summary>
        /// Type 3 fonts are usually unnamed.
        /// </summary>
        public NameToken Name { get; }

        public bool IsVertical { get; } = false;

        public FontDetails Details { get; }

        public Type3Font(NameToken name, PdfRectangle boundingBox, TransformationMatrix fontMatrix,
            Encoding encoding, int firstChar, int lastChar, double[] widths,
            CMap toUnicodeCMap, IReadOnlyDictionary<string, IReadOnlyList<IGraphicsStateOperation>> charProcs)
        {
            Name = name;

            this.boundingBox = boundingBox;
            this.fontMatrix = fontMatrix;
            this.encoding = encoding;
            this.firstChar = firstChar;
            this.lastChar = lastChar;
            this.widths = widths;
            this.toUnicodeCMap = new ToUnicodeCMap(toUnicodeCMap);
            this.charProcs = charProcs;
            Details = FontDetails.GetDefault(name?.Data);
            isZapfDingbats = encoding is ZapfDingbatsEncoding || Details.Name.Contains("ZapfDingbats");
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, [NotNullWhen(true)] out string? value)
        {
            value = null;

            if (toUnicodeCMap.CanMapToUnicode && toUnicodeCMap.TryGet(characterCode, out value))
            {
                return true;
            }

            if (encoding is null)
            {
                return false;
            }

            try
            {
                var name = encoding.GetName(characterCode);

                if (isZapfDingbats)
                {
                    value = GlyphList.ZapfDingbats.NameToUnicode(name);
                    if (value is not null)
                    {
                        return true;
                    }
                }

                value = GlyphList.AdobeGlyphList.NameToUnicode(name);
            }
            catch
            {
                return false;
            }

            return value is not null;
        }

        public CharacterBoundingBox GetBoundingBox(int characterCode)
        {
            var characterBoundingBox = GetBoundingBoxInGlyphSpace(characterCode);

            characterBoundingBox = fontMatrix.Transform(characterBoundingBox);

            var width = fontMatrix.TransformX(widths[characterCode - firstChar]);

            return new CharacterBoundingBox(characterBoundingBox, width);
        }

        private PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode)
        {
            if (characterCode < firstChar || characterCode > lastChar)
            {
                throw new InvalidFontFormatException($"The character code was not contained in the widths array: {characterCode}.");
            }

            var name = encoding.GetName(characterCode);
            if (charProcs.TryGetValue(name, out var operations))
            {
                // https://github.com/apache/pdfbox/blob/trunk/pdfbox/src/main/java/org/apache/pdfbox/pdmodel/font/PDType3CharProc.java
                foreach (var operation in operations)
                {
                    /*
                    if (operation is Type3SetGlyphWidthAndBoundingBox bbox)
                    {
                        return new PdfRectangle(bbox.LowerLeftX,
                            bbox.LowerLeftX,
                            bbox.UpperRightX - bbox.LowerLeftX,
                            bbox.UpperRightY - bbox.LowerLeftX);
                    }
                    */
                }
                // See also https://github.com/apache/pdfbox/blob/trunk/debugger/src/main/java/org/apache/pdfbox/debugger/fontencodingpane/Type3Font.java 
                // for more details
            }

            return new PdfRectangle(0, 0, widths[characterCode - firstChar], boundingBox.Height);
        }

        public TransformationMatrix GetFontMatrix()
        {
            return fontMatrix;
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>Type 3 fonts do not use vector paths. Always returns <c>false</c>.</para>
        /// </summary>
        public bool TryGetPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            path = null;
            return false;
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>Type 3 fonts do not use vector paths. Always returns <c>false</c>.</para>
        /// </summary>
        public bool TryGetNormalisedPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            return TryGetPath(characterCode, out path);
        }

        public bool TryRender(int characterCode, IOperationContext operationContext)
        {
            var name = encoding.GetName(characterCode);
            if (charProcs.TryGetValue(name, out var operations))
            {
                // https://github.com/apache/pdfbox/blob/3007890b9545f412845bd7f94e32be5de5c55666/debugger/src/main/java/org/apache/pdfbox/debugger/fontencodingpane/Type3Font.java#L155
                operationContext.PushState();

                //             page.setResources(resources);

                //var matrix = operationContext.GetCurrentState().CurrentTransformationMatrix;
                //operationContext.GetCurrentState().CurrentTransformationMatrix = TransformationMatrix.Identity;

                foreach (var operation in operations)
                {
                    operation.Run(operationContext);
                }

                operationContext.PopState();
                return true;
            }

            return false;
        }
    }
}
