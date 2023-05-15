namespace UglyToad.PdfPig.Content
{
    using Core;
    using Filters;
    using Graphics;
    using Graphics.Operations;
    using System;
    using System.Collections.Generic;
    using Tokenization.Scanner;
    using UglyToad.PdfPig.Geometry;
    using UglyToad.PdfPig.Parser;
    using XObjects;

    /// <summary>
    /// Wraps content parsed from a page content stream for access.
    /// </summary>
    /// <remarks>
    /// This should contain a replayable stack of drawing instructions for page content
    /// from a content stream in addition to lazily evaluated state such as text on the page or images.
    /// </remarks>
    public class PageContent
    {
        private readonly IReadOnlyList<Union<XObjectContentRecord, InlineImage>> images;
        private readonly IReadOnlyList<MarkedContentElement> markedContents;

        /// <summary>
        /// TOOD
        /// </summary>
        public readonly IPdfTokenScanner pdfScanner;

        /// <summary>
        /// TOOD
        /// </summary>
        public readonly ILookupFilterProvider filterProvider;

        /// <summary>
        /// TODO
        /// </summary>
        public readonly IPageContentParser pageContentParser;

        /// <summary>
        /// TOOD
        /// </summary>
        public readonly IResourceStore resourceStore;

        internal IReadOnlyList<IGraphicsStateOperation> GraphicsStateOperations { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public IReadOnlyList<Letter> Letters { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public IReadOnlyList<PdfPath> Paths { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public int NumberOfImages => images.Count;

        /// <summary>
        /// TODO
        /// </summary>
        public UserSpaceUnit UserSpaceUnit { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public InternalParsingOptions ParsingOptions { get; }

        internal PageContent(IReadOnlyList<IGraphicsStateOperation> graphicsStateOperations,
            IReadOnlyList<Letter> letters,
            IReadOnlyList<PdfPath> paths,
            IReadOnlyList<Union<XObjectContentRecord, InlineImage>> images,
            IReadOnlyList<MarkedContentElement> markedContents,
            IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser,
            ILookupFilterProvider filterProvider,
            IResourceStore resourceStore,
            UserSpaceUnit userSpaceUnit,
            InternalParsingOptions parsingOptions)
        {
            GraphicsStateOperations = graphicsStateOperations;
            Letters = letters;
            Paths = paths;
            this.images = images;
            this.markedContents = markedContents;
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
            this.pageContentParser = pageContentParser ?? throw new ArgumentNullException(nameof(resourceStore));
            UserSpaceUnit = userSpaceUnit;
            ParsingOptions = parsingOptions;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public IEnumerable<IPdfImage> GetImages()
        {
            foreach (var image in images)
            {
                if (image.TryGetFirst(out var xObjectContentRecord))
                {
                    yield return XObjectFactory.ReadImage(xObjectContentRecord, pdfScanner, filterProvider, resourceStore);
                }
                else if (image.TryGetSecond(out var inlineImage))
                {
                    yield return inlineImage;
                }
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public IReadOnlyList<MarkedContentElement> GetMarkedContents() => markedContents;
    }
}
