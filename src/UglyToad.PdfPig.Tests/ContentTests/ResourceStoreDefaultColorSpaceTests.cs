namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Graphics.Colors;
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;
    using UglyToad.PdfPig.Tests.Tokens;
    using Xunit;

    public class ResourceStoreDefaultColorSpaceTests
    {
        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
        }

        private static ResourceStore BuildStore()
        {
            return new ResourceStore(
                new TestPdfTokenScanner(),
                new NoOpFontFactory(),
                new TestFilterProvider(),
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                });
        }

        [Fact]
        public void DeviceRgbRequest_WithDefaultRgbInResources_UsesDefaultRgb()
        {
            // Resources/ColorSpace/DefaultRGB -> [ /CalRGB << /WhitePoint [0.9505 1 1.089] >> ]
            var calRgbDict = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.WhitePoint,
                    new ArrayToken(new IToken[]
                    {
                        new NumericToken(0.9505),
                        new NumericToken(1.0),
                        new NumericToken(1.089),
                    })
                },
            });
            var defaultRgbArray = new ArrayToken(new IToken[] { NameToken.Calrgb, calRgbDict });

            var resources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.ColorSpace,
                    new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Create("DefaultRGB"), defaultRgbArray },
                    })
                },
            });

            var store = BuildStore();
            store.LoadResourceDictionary(resources);

            var details = store.GetColorSpaceDetails(
                NameToken.Devicergb,
                new DictionaryToken(new Dictionary<NameToken, IToken>()));

            Assert.Equal(ColorSpace.CalRGB, details.Type);
        }
    }
}
