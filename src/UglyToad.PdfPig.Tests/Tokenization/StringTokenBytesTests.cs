using System.Linq;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Tokenization;
using UglyToad.PdfPig.Tokenization.Scanner;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Tests.Tokenization
{
    /// <summary>
    /// A string token read from a file keeps the bytes it was read from, whatever encoding its text
    /// was decoded with, so a decoding that cannot be reversed exactly does not lose them. Most of
    /// these hold for the earlier behaviour too, where the bytes were kept only for the encodings
    /// marked by a byte order mark and re-encoded from the text otherwise; they are here to hold
    /// the invariant still while the consumers of these tokens move over to the bytes.
    /// </summary>
    public class StringTokenBytesTests
    {
        private static StringToken Tokenize(bool usePdfDocEncoding, params byte[] literalContents)
        {
            // The tokenizer is handed the bytes between the brackets, having already read the '('.
            var input = new MemoryInputBytes(literalContents.Concat(new byte[] { (byte)')' }).ToArray());

            var tokenizer = new StringTokenizer(usePdfDocEncoding);

            Assert.True(tokenizer.TryTokenize((byte)'(', input, out var token));

            return Assert.IsType<StringToken>(token);
        }

        [Fact]
        public void ContentStreamStringKeepsItsBytes()
        {
            // Character codes for the current font, which must survive untouched.
            var bytes = new byte[] { 0x00, 0x41, 0xFE, 0xFF, 0x80 };

            var token = Tokenize(usePdfDocEncoding: false, bytes);

            Assert.Equal(StringToken.Encoding.Iso88591, token.EncodedWith);
            Assert.Equal(bytes, token.GetBytes());
            Assert.Equal(bytes, token.Bytes.ToArray());
        }

        [Fact]
        public void PdfDocEncodedStringKeepsItsBytes()
        {
            var bytes = new byte[] { 0x48, 0x69, 0x18 }; // "Hi" then a breve, which PdfDocEncoding maps

            var token = Tokenize(usePdfDocEncoding: true, bytes);

            Assert.Equal(StringToken.Encoding.PdfDocEncoding, token.EncodedWith);
            Assert.Equal(bytes, token.GetBytes());
        }

        [Fact]
        public void Utf16BigEndianStringKeepsItsBytes()
        {
            var bytes = new byte[] { 0xFE, 0xFF, 0x00, 0x48, 0x00, 0x69 };

            var token = Tokenize(usePdfDocEncoding: true, bytes);

            Assert.Equal(StringToken.Encoding.Utf16BE, token.EncodedWith);
            Assert.Equal("Hi", token.Data);
            Assert.Equal(bytes, token.GetBytes());
        }

        [Fact]
        public void Utf8StringKeepsItsBytes()
        {
            var bytes = new byte[] { 0xEF, 0xBB, 0xBF, 0x48, 0x69 };

            var token = Tokenize(usePdfDocEncoding: true, bytes);

            Assert.Equal(StringToken.Encoding.Utf8, token.EncodedWith);
            Assert.Equal("Hi", token.Data);
            Assert.Equal(bytes, token.GetBytes());
        }

        /// <summary>
        /// An unpaired surrogate does not survive being decoded and encoded again, so this is the
        /// case that shows the bytes are kept rather than reconstructed.
        /// </summary>
        [Fact]
        public void Utf16StringWithAnUnpairedSurrogateKeepsItsBytes()
        {
            var bytes = new byte[] { 0xFE, 0xFF, 0x00, 0x41, 0xD8, 0x00 };

            var token = Tokenize(usePdfDocEncoding: true, bytes);

            Assert.Equal(StringToken.Encoding.Utf16BE, token.EncodedWith);
            Assert.Equal(bytes, token.GetBytes());
        }

        /// <summary>
        /// A token created from a string has no bytes of its own, so they are encoded from the text
        /// as before.
        /// </summary>
        [Fact]
        public void StringCreatedFromTextStillEncodesItsBytes()
        {
            var token = new StringToken("Hi");

            Assert.Equal(new byte[] { 0x48, 0x69 }, token.GetBytes());
            Assert.Equal(new byte[] { 0x48, 0x69 }, token.Bytes.ToArray());
        }

        [Fact]
        public void BytesAreEncodedOnlyOnce()
        {
            var token = new StringToken("Hi");

            Assert.Same(token.GetBytes(), token.GetBytes());
        }
    }
}
