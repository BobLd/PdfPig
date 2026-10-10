namespace UglyToad.PdfPig.Artwork
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Annotations;
    using Core;
    using Filters;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// 3D artwork embedded in a PDF document: the 3D stream of a 3D annotation (ISO 32000-2, 13.6) or the
    /// 3D asset of a rich media annotation (13.7), together with its views and activation settings.
    /// <para>
    /// This is a read-only description. It decodes no 3D data: <see cref="GetData"/> hands out the U3D or
    /// PRC file as bytes, and scripts are exposed as bytes and never run.
    /// </para>
    /// </summary>
    public sealed class Artwork3D
    {
        private readonly IPdfTokenScanner scanner;
        private readonly ILookupFilterProvider filterProvider;
        private readonly StreamToken stream;
        private readonly StreamToken? script;
        private readonly ArrayToken? viewArray;
        private readonly IToken? annotationDefaultView;
        private readonly IToken? streamDefaultView;
        private readonly object sync = new object();

        private Artwork3DView[]? views;
        private Artwork3DView? defaultView;
        private bool defaultViewResolved;

        /// <summary>
        /// The kind of annotation the artwork was found in.
        /// </summary>
        public Artwork3DSource Source { get; }

        /// <summary>
        /// The format of the 3D data. <see cref="Artwork3DFormat.Unknown"/> means the annotation should be
        /// left showing its normal appearance.
        /// </summary>
        public Artwork3DFormat Format { get; }

        /// <summary>
        /// The format name as written in the 3D stream (<c>Subtype</c>), if any. For rich media, the file
        /// name of the asset.
        /// </summary>
        public string? FormatName { get; }

        /// <summary>
        /// Identifies a run-time instance shared between annotations: the reference of the 3D reference
        /// dictionary when the annotation uses one. <see langword="null"/> when the annotation refers to the
        /// 3D stream directly and so has an instance of its own.
        /// </summary>
        public IndirectReference? InstanceKey { get; }

        /// <summary>
        /// The reference of the stream holding the 3D data, when it is an indirect object. Equal for
        /// annotations that reuse the same data.
        /// </summary>
        public IndirectReference? StreamReference { get; }

        /// <summary>
        /// The dictionary of the stream holding the 3D data.
        /// </summary>
        public DictionaryToken StreamDictionary => stream.StreamDictionary;

        /// <summary>
        /// The length in bytes of the 3D data as stored, before any stream filter is removed.
        /// </summary>
        public long EncodedLength => stream.Data.Length;

        /// <summary>
        /// When the artwork is activated and deactivated, and how it is presented.
        /// </summary>
        public Artwork3DActivation Activation { get; }

        /// <summary>
        /// The 3D view box: the rectangle in which the artwork is drawn, in the annotation's target
        /// coordinate system, whose origin is the centre of the annotation's rectangle (<c>3DB</c>).
        /// <see langword="null"/> when absent, meaning the whole annotation rectangle.
        /// </summary>
        public PdfRectangle? ViewBox { get; }

        /// <summary>
        /// Whether the artwork is intended to be manipulated interactively by the user rather than by a
        /// script (<c>3DI</c>). Default <see langword="true"/>.
        /// </summary>
        public bool IsInteractive { get; }

        /// <summary>
        /// How keyframe animations in the artwork are driven (<c>AN</c>).
        /// </summary>
        public Artwork3DAnimationStyle AnimationStyle { get; }

        /// <summary>
        /// The 3D units dictionary (<c>3DU</c>, PDF 2.0), unparsed, if any.
        /// </summary>
        public DictionaryToken? Units { get; }

        /// <summary>
        /// The colour space in which the artwork's colour values are encoded (<c>ColorSpace</c>, PDF 2.0),
        /// as written, if any.
        /// </summary>
        public IToken? ColorSpace { get; }

        /// <summary>
        /// Whether the artwork carries a script to run when it is instantiated (<c>OnInstantiate</c>).
        /// </summary>
        public bool HasScript => script != null;

        /// <summary>
        /// The named views of the artwork, in the order they should be presented. Parsed on first access.
        /// </summary>
        public IReadOnlyList<Artwork3DView> Views
        {
            get
            {
                var current = views;
                if (current != null)
                {
                    return current;
                }

                lock (sync)
                {
                    return views ??= Artwork3DParser.ParseViews(viewArray, scanner);
                }
            }
        }

        /// <summary>
        /// The view to display initially: the annotation's choice, else the stream's, else the first of
        /// <see cref="Views"/>. It may be a view that is not in <see cref="Views"/>.
        /// <see langword="null"/> means the view defined inside the 3D data itself should be used.
        /// </summary>
        public Artwork3DView? DefaultView
        {
            get
            {
                lock (sync)
                {
                    if (!defaultViewResolved)
                    {
                        defaultView = ResolveDefaultView();
                        defaultViewResolved = true;
                    }

                    return defaultView;
                }
            }
        }

        private Artwork3D(
            IPdfTokenScanner scanner,
            ILookupFilterProvider filterProvider,
            Artwork3DSource source,
            Artwork3DFormat format,
            string? formatName,
            StreamToken stream,
            IndirectReference? instanceKey,
            IndirectReference? streamReference,
            ArrayToken? viewArray,
            IToken? annotationDefaultView,
            IToken? streamDefaultView,
            Artwork3DActivation activation,
            PdfRectangle? viewBox,
            bool isInteractive,
            Artwork3DAnimationStyle animationStyle,
            DictionaryToken? units,
            IToken? colorSpace,
            StreamToken? script)
        {
            this.scanner = scanner;
            this.filterProvider = filterProvider;
            this.stream = stream;
            this.viewArray = viewArray;
            this.annotationDefaultView = annotationDefaultView;
            this.streamDefaultView = streamDefaultView;
            this.script = script;
            Source = source;
            Format = format;
            FormatName = formatName;
            InstanceKey = instanceKey;
            StreamReference = streamReference;
            Activation = activation;
            ViewBox = viewBox;
            IsInteractive = isInteractive;
            AnimationStyle = animationStyle;
            Units = units;
            ColorSpace = colorSpace;
        }

        /// <summary>
        /// Try to read the 3D artwork of an annotation.
        /// </summary>
        /// <param name="annotation">The annotation, of any type.</param>
        /// <param name="scanner">The token scanner of the document the annotation belongs to.</param>
        /// <param name="filterProvider">The filter provider of that document.</param>
        /// <param name="artwork">The artwork, when the method returns <see langword="true"/>.</param>
        /// <returns>
        /// <see langword="false"/> if the annotation is neither a 3D annotation with a 3D stream nor a rich
        /// media annotation with 3D content. Never throws on the content of the document.
        /// </returns>
        public static bool TryCreate(
            Annotation annotation,
            IPdfTokenScanner scanner,
            ILookupFilterProvider filterProvider,
            [NotNullWhen(true)] out Artwork3D? artwork)
        {
            if (annotation is null)
            {
                throw new ArgumentNullException(nameof(annotation));
            }

            if (scanner is null)
            {
                throw new ArgumentNullException(nameof(scanner));
            }

            if (filterProvider is null)
            {
                throw new ArgumentNullException(nameof(filterProvider));
            }

            switch (annotation.Type)
            {
                case AnnotationType.Artwork3D:
                    return TryCreateFromAnnotation3D(annotation.AnnotationDictionary, scanner, filterProvider, out artwork);
                default:
                    artwork = null;
                    return false;
            }
        }

        /// <summary>
        /// The 3D data: a complete U3D or PRC file, with the stream's filters removed. Decoded on every
        /// call; the result is not cached here.
        /// </summary>
        public ReadOnlyMemory<byte> GetData()
        {
            return stream.Decode(filterProvider, scanner);
        }

        /// <summary>
        /// The script to run when the artwork is instantiated, as bytes. Empty when there is none.
        /// Nothing in this library executes it.
        /// </summary>
        public ReadOnlyMemory<byte> GetScript()
        {
            return script is null ? ReadOnlyMemory<byte>.Empty : script.Decode(filterProvider, scanner);
        }

        /// <inheritdoc />
        public override string ToString() => $"{Source} {Format}";

        private static bool TryCreateFromAnnotation3D(
            DictionaryToken annotationDictionary,
            IPdfTokenScanner scanner,
            ILookupFilterProvider filterProvider,
            [NotNullWhen(true)] out Artwork3D? artwork)
        {
            artwork = null;

            if (!annotationDictionary.TryGet(Artwork3DNames.ThreeDD, out IToken? raw))
            {
                return false;
            }

            IndirectReference? instanceKey = null;
            var streamReference = (raw as IndirectReferenceToken)?.Data;
            var resolved = Artwork3DParser.Resolve(raw, scanner);

            if (!(resolved is StreamToken stream))
            {
                // A 3D reference dictionary: annotations naming the same one share a run-time instance.
                if (!(resolved is DictionaryToken referenceDictionary)
                    || !referenceDictionary.TryGet(Artwork3DNames.ThreeDD, out IToken? inner)
                    || !(Artwork3DParser.Resolve(inner, scanner) is StreamToken referencedStream))
                {
                    return false;
                }

                instanceKey = streamReference;
                streamReference = (inner as IndirectReferenceToken)?.Data;
                stream = referencedStream;
            }

            var streamDictionary = stream.StreamDictionary;
            var formatName = Artwork3DParser.GetName(streamDictionary, NameToken.Subtype, scanner);

            annotationDictionary.TryGet(Artwork3DNames.ThreeDV, out IToken? annotationDefaultView);
            streamDictionary.TryGet(Artwork3DNames.DV, out IToken? streamDefaultView);
            streamDictionary.TryGet(NameToken.ColorSpace, out IToken? colorSpace);

            PdfRectangle? viewBox = null;
            var box = new double[4];
            if (Artwork3DParser.TryGetNumbers(Artwork3DParser.GetArray(annotationDictionary, Artwork3DNames.ThreeDB, scanner), scanner, 0, 4, box))
            {
                viewBox = new PdfRectangle(box[0], box[1], box[2], box[3]);
            }

            var activationDictionary = Artwork3DParser.GetDictionary(annotationDictionary, Artwork3DNames.ThreeDA, scanner);
            var animationDictionary = Artwork3DParser.GetDictionary(streamDictionary, Artwork3DNames.AN, scanner);

            artwork = new Artwork3D(
                scanner,
                filterProvider,
                Artwork3DSource.Annotation3D,
                ParseFormat(formatName),
                formatName,
                stream,
                instanceKey,
                streamReference,
                Artwork3DParser.GetArray(streamDictionary, Artwork3DNames.VA, scanner),
                annotationDefaultView,
                streamDefaultView,
                activationDictionary is null ? Artwork3DActivation.Default : ParseActivation(activationDictionary, scanner),
                viewBox,
                Artwork3DParser.GetBoolean(annotationDictionary, Artwork3DNames.ThreeDI, scanner) ?? true,
                animationDictionary is null ? Artwork3DAnimationStyle.None : ParseAnimationStyle(animationDictionary, scanner),
                Artwork3DParser.GetDictionary(annotationDictionary, Artwork3DNames.ThreeDU, scanner),
                colorSpace,
                Artwork3DParser.Get(streamDictionary, Artwork3DNames.OnInstantiate, scanner) as StreamToken);

            return true;
        }

        private static Artwork3DFormat ParseFormat(string? subtype)
        {
            switch (subtype)
            {
                case "U3D":
                    return Artwork3DFormat.U3D;
                case "PRC":
                    return Artwork3DFormat.Prc;
                default:
                    return Artwork3DFormat.Unknown;
            }
        }

        private static Artwork3DActivation ParseActivation(DictionaryToken dictionary, IPdfTokenScanner scanner)
        {
            var defaults = Artwork3DActivation.Default;

            var mode = defaults.Mode;
            switch (Artwork3DParser.GetName(dictionary, NameToken.A, scanner))
            {
                case "PO":
                    mode = Artwork3DActivationMode.PageOpen;
                    break;
                case "PV":
                    mode = Artwork3DActivationMode.PageVisible;
                    break;
            }

            var deactivationMode = defaults.DeactivationMode;
            switch (Artwork3DParser.GetName(dictionary, NameToken.D, scanner))
            {
                case "PC":
                    deactivationMode = Artwork3DDeactivationMode.PageClose;
                    break;
                case "XD":
                    deactivationMode = Artwork3DDeactivationMode.Explicit;
                    break;
            }

            return new Artwork3DActivation(
                mode,
                ParseInstanceState(Artwork3DParser.GetName(dictionary, Artwork3DNames.AIS, scanner), defaults.ActivationState, allowUninstantiated: false),
                deactivationMode,
                ParseInstanceState(Artwork3DParser.GetName(dictionary, Artwork3DNames.DIS, scanner), defaults.DeactivationState, allowUninstantiated: true),
                Artwork3DParser.GetBoolean(dictionary, Artwork3DNames.TB, scanner) ?? defaults.ShowToolbar,
                Artwork3DParser.GetBoolean(dictionary, Artwork3DNames.NP, scanner) ?? defaults.ShowNavigationPanel,
                ParseStyle(Artwork3DParser.GetName(dictionary, Artwork3DNames.Style, scanner)),
                Artwork3DParser.GetDictionary(dictionary, Artwork3DNames.Window, scanner),
                Artwork3DParser.GetBoolean(dictionary, Artwork3DNames.Transparent, scanner) ?? defaults.Transparent);
        }

        private static Artwork3DInstanceState ParseInstanceState(string? name, Artwork3DInstanceState fallback, bool allowUninstantiated)
        {
            switch (name)
            {
                case "I":
                    return Artwork3DInstanceState.Instantiated;
                case "L":
                    return Artwork3DInstanceState.Live;
                case "U" when allowUninstantiated:
                    return Artwork3DInstanceState.Uninstantiated;
                default:
                    return fallback;
            }
        }

        private static Artwork3DPresentationStyle ParseStyle(string? name)
        {
            return name == "Windowed" ? Artwork3DPresentationStyle.Windowed : Artwork3DPresentationStyle.Embedded;
        }

        private static Artwork3DAnimationStyle ParseAnimationStyle(DictionaryToken dictionary, IPdfTokenScanner scanner)
        {
            Artwork3DAnimationType type;
            switch (Artwork3DParser.GetName(dictionary, NameToken.Subtype, scanner))
            {
                case "Linear":
                    type = Artwork3DAnimationType.Linear;
                    break;
                case "Oscillating":
                    type = Artwork3DAnimationType.Oscillating;
                    break;
                default:
                    type = Artwork3DAnimationType.None;
                    break;
            }

            var playCount = Artwork3DParser.GetNumber(dictionary, Artwork3DNames.PC, scanner) ?? 0;
            if (double.IsNaN(playCount) || playCount > int.MaxValue || playCount < int.MinValue)
            {
                playCount = 0;
            }

            return new Artwork3DAnimationStyle(type, (int)playCount, Artwork3DParser.GetNumber(dictionary, Artwork3DNames.TM, scanner) ?? 1);
        }

        private Artwork3DView? ResolveDefaultView()
        {
            var found = ResolveViewReference(annotationDefaultView, allowStreamDefault: true, out var view);
            if (found != ViewReference.Found)
            {
                found = ResolveViewReference(streamDefaultView, allowStreamDefault: false, out view);
            }

            if (found == ViewReference.Found)
            {
                return view;
            }

            var all = Views;
            return all.Count > 0 ? all[0] : null;
        }

        private enum ViewReference
        {
            NotFound,
            Found,
            UseStreamDefault
        }

        /// <summary>
        /// A default view may be given as a view dictionary, an index into the view array, a text string
        /// matching a view's internal name, or a name: F first, L last, D the stream's default.
        /// </summary>
        private ViewReference ResolveViewReference(IToken? raw, bool allowStreamDefault, out Artwork3DView? view)
        {
            view = null;
            var all = Views;

            switch (Artwork3DParser.Resolve(raw, scanner))
            {
                case DictionaryToken dictionary:
                    return Artwork3DParser.TryParseView(dictionary, scanner, out view) ? ViewReference.Found : ViewReference.NotFound;

                case NumericToken number:
                    if (!number.HasDecimalPlaces && number.Data >= 0 && number.Data < all.Count)
                    {
                        view = all[(int)number.Data];
                        return ViewReference.Found;
                    }

                    return ViewReference.NotFound;

                // NameToken is also a string-valued token: it must be tested before the text strings.
                case NameToken name:
                    if (name.Data == "F" && all.Count > 0)
                    {
                        view = all[0];
                        return ViewReference.Found;
                    }

                    if (name.Data == "L" && all.Count > 0)
                    {
                        view = all[all.Count - 1];
                        return ViewReference.Found;
                    }

                    return name.Data == "D" && allowStreamDefault ? ViewReference.UseStreamDefault : ViewReference.NotFound;

                case IToken token when Artwork3DParser.AsText(token) is string text:
                    for (var i = 0; i < all.Count; i++)
                    {
                        if (all[i].InternalName == text)
                        {
                            view = all[i];
                            return ViewReference.Found;
                        }
                    }

                    for (var i = 0; i < all.Count; i++)
                    {
                        if (all[i].ExternalName == text)
                        {
                            view = all[i];
                            return ViewReference.Found;
                        }
                    }

                    return ViewReference.NotFound;

                default:
                    return ViewReference.NotFound;
            }
        }
    }
}
