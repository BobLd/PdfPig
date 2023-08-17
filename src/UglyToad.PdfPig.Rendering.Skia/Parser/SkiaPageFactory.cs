using SkiaSharp;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Graphics.Operations;
using UglyToad.PdfPig.Logging;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Parser;
using UglyToad.PdfPig.Rendering.Skia.Graphics;
using UglyToad.PdfPig.Tokenization.Scanner;
using UglyToad.PdfPig.Tokens;
using UglyToad.PdfPig.PdfFonts;

namespace UglyToad.PdfPig.Rendering.Skia.Parser
{
    public sealed class SkiaPageFactory : PageFactoryBase<SKPicture>
    {
        public SkiaPageFactory(
            IFontFactory fontFactory,
            IPdfTokenScanner pdfScanner,
            ILookupFilterProvider filterProvider,
            IPageContentParser pageContentParser,
            ILog log)
                : base(fontFactory, pdfScanner, filterProvider, pageContentParser, log)
        { }

        protected override SKPicture ProcessPage(
            int pageNumber,
            IResourceStore pageResourceStore,
            DictionaryToken dictionary,
            NamedDestinations namedDestinations,
            IReadOnlyList<byte> contentBytes,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            MediaBox mediaBox,
            IParsingOptions parsingOptions)
        {
            var context = new SkiaStreamProcessor(pageNumber, pageResourceStore, userSpaceUnit, mediaBox, cropBox, rotation, PdfScanner, PageContentParser, FilterProvider, parsingOptions);

            var operations = PageContentParser.Parse(pageNumber, new ByteArrayInputBytes(contentBytes), parsingOptions.Logger);
            return context.Process(pageNumber, operations);
        }

        protected override SKPicture ProcessPage(
            int pageNumber,
            IResourceStore pageResourceStore,
            DictionaryToken dictionary,
            NamedDestinations namedDestinations,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            MediaBox mediaBox,
            IParsingOptions parsingOptions)
        {
            var context = new SkiaStreamProcessor(pageNumber, pageResourceStore, userSpaceUnit, mediaBox, cropBox, rotation, PdfScanner, PageContentParser, FilterProvider, parsingOptions);
            return context.Process(pageNumber, EmptyArray<IGraphicsStateOperation>.Instance);
        }
    }
}
