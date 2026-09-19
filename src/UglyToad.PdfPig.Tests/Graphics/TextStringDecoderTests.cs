using System.Globalization;
using System.IO;
using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Tokens;
using UglyToad.PdfPig.Util;

namespace UglyToad.PdfPig.Tests.Graphics
{
    /// <summary>
    /// A content stream is tokenized with <c>usePdfDocEncoding: false</c>, because the operand of a
    /// text-showing operator is a sequence of character codes rather than text. That setting belongs
    /// to the scanner, so it reaches the dictionary tokenizer too and leaves entries such as
    /// <c>/ActualText</c> - which the specification types as text strings - undecoded.
    /// </summary>
    public class TextStringDecoderTests
    {
        /// <summary>
        /// A literal string as the content stream tokenizer produces it: one character per byte.
        /// </summary>
        private static StringToken RawLiteral(params byte[] bytes)
        {
            return new StringToken(OtherEncodings.BytesAsLatin1String(bytes), StringToken.Encoding.Iso88591);
        }

        [Fact]
        public void DecodesUtf16BigEndianByteOrderMark()
        {
            // (\xFE\xFF\x00\x20) is the spec-correct spelling of a single space.
            var token = RawLiteral(0xFE, 0xFF, 0x00, 0x20);

            Assert.Equal(" ", TextStringDecoder.Decode(token));
        }

        [Fact]
        public void DecodesUtf16BigEndianTextBeyondLatin1()
        {
            // "H", "i", then U+4E2D.
            var token = RawLiteral(0xFE, 0xFF, 0x00, 0x48, 0x00, 0x69, 0x4E, 0x2D);

            Assert.Equal("Hi中", TextStringDecoder.Decode(token));
        }

        [Fact]
        public void DecodesUtf8ByteOrderMark()
        {
            var token = RawLiteral(0xEF, 0xBB, 0xBF, 0x48, 0x69);

            Assert.Equal("Hi", TextStringDecoder.Decode(token));
        }

        [Fact]
        public void DecodesUtf16LittleEndianByteOrderMark()
        {
            var token = RawLiteral(0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00);

            Assert.Equal("Hi", TextStringDecoder.Decode(token));
        }

        [Fact]
        public void WithoutAByteOrderMarkUsesPdfDocEncoding()
        {
            var token = RawLiteral(0x48, 0x69);

            Assert.Equal("Hi", TextStringDecoder.Decode(token));
        }

        [Fact]
        public void LeavesAHexTokenAlone()
        {
            // HexToken applies the byte order mark rules whatever the scanner was doing.
            var token = new HexToken("FEFF0048".AsSpan());

            Assert.Equal("H", token.Data);
            Assert.Equal("H", TextStringDecoder.Decode(token));
        }

        [Fact]
        public void LeavesAnAlreadyDecodedStringTokenAlone()
        {
            // As produced by the object scanner, which decodes text strings itself.
            var token = new StringToken("Hi", StringToken.Encoding.Utf16BE);

            Assert.Equal("Hi", TextStringDecoder.Decode(token));
        }

        [Fact]
        public void LeavesANameTokenAlone()
        {
            Assert.Equal("Span", TextStringDecoder.Decode(NameToken.Create("Span")));
        }

        /// <summary>
        /// End to end: a marked-content sequence whose properties are given inline, with the
        /// replacement text in the spec-correct UTF-16BE form.
        /// </summary>
        [Fact]
        public void InlineMarkedContentPropertiesDecodeTheirTextStrings()
        {
            var contents = new MemoryStream();

            void WriteAscii(string s)
            {
                var b = Encoding.ASCII.GetBytes(s);
                contents.Write(b, 0, b.Length);
            }

            WriteAscii("/Span <</ActualText (");
            // A byte order mark followed by "Hi" in UTF-16BE. None of these bytes need escaping.
            contents.Write([0xFE, 0xFF, 0x00, 0x48, 0x00, 0x69], 0, 6);
            WriteAscii(")>> BDC\nEMC\n");

            using var document = PdfDocument.Open(BuildSinglePagePdf(contents.ToArray()));

            var markedContent = Assert.Single(document.GetPage(1).GetMarkedContents());

            Assert.Equal("Hi", markedContent.ActualText);
        }

        private static byte[] BuildSinglePagePdf(byte[] contentStream)
        {
            using var ms = new MemoryStream();
            var offsets = new long[5];

            void Write(string s)
            {
                var b = Encoding.ASCII.GetBytes(s);
                ms.Write(b, 0, b.Length);
            }

            Write("%PDF-1.7\n");

            offsets[1] = ms.Position;
            Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            offsets[2] = ms.Position;
            Write("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            offsets[3] = ms.Position;
            Write("3 0 obj\n<< /Type /Page /Parent 2 0 R /Resources << >> "
                  + "/MediaBox [0 0 100 100] /Contents 4 0 R >>\nendobj\n");

            offsets[4] = ms.Position;
            Write($"4 0 obj\n<< /Length {contentStream.Length.ToString(CultureInfo.InvariantCulture)} >>\nstream\n");
            ms.Write(contentStream, 0, contentStream.Length);
            Write("\nendstream\nendobj\n");

            var xref = ms.Position;
            Write("xref\n0 5\n");
            Write("0000000000 65535 f \n");
            for (int i = 1; i <= 4; i++)
            {
                Write($"{offsets[i].ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
            }

            Write("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n");
            Write(xref.ToString(CultureInfo.InvariantCulture));
            Write("\n%%EOF\n");

            return ms.ToArray();
        }
    }
}
