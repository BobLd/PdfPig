namespace UglyToad.PdfPig.Tests.ContentTests
{
    using PdfPig.Content;
    using PdfPig.Tokens;
    using UglyToad.PdfPig.Tests.Tokens;

    public class XmpMetadataTests
    {
        [Fact]
        public void EqualsAndGetHashCode()
        {
            var stream1 = new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>()), new byte[] { 1, 2, 3 });
            var stream2 = new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>()), new byte[] { 1, 2, 3 });
            var stream3 = new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>()), new byte[] { 4, 5, 6 });

            var filterProvider = TestFilterProvider.Instance;
            var scanner = new TestPdfTokenScanner();

            var xmp1 = new XmpMetadata(stream1, filterProvider, scanner);
            var xmp2 = new XmpMetadata(stream2, filterProvider, scanner);
            var xmp3 = new XmpMetadata(stream3, filterProvider, scanner);

            Assert.Equal(xmp1, xmp2);
            Assert.Equal(xmp1.GetHashCode(), xmp2.GetHashCode());
            Assert.NotEqual(xmp1, xmp3);
            Assert.False(xmp1.Equals(null));
        }
    }
}
