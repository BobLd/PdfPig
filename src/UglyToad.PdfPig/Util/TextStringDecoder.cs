namespace UglyToad.PdfPig.Util
{
    using System.Text;
    using Core;
    using Tokens;

    /// <summary>
    /// Decodes a token the specification types as a <i>text string</i> (7.9.2.2) but which was read
    /// out of a content stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A content stream is tokenized with <c>usePdfDocEncoding: false</c>, because the operand of a
    /// text-showing operator is a sequence of character codes for the current font rather than text
    /// and those bytes must survive verbatim. That setting belongs to the scanner, so it also
    /// reaches the dictionary tokenizer, and a string inside an inline marked-content property
    /// dictionary is left raw as well. Some of those entries are text strings though, so a producer
    /// writing the spec-correct UTF-16BE form of a space, <c>(\xFE\xFF\x00\x20)</c>, gets back four
    /// ISO 8859-1 characters (U+00FE, U+00FF, U+0000, U+0020) rather than one space.
    /// </para>
    /// <para>
    /// The same value escapes this because it took a different route: <see cref="HexToken"/> applies
    /// the byte order mark rules whatever the scanner was doing, so <c>&lt;FEFF0020&gt;</c> decodes,
    /// and a property dictionary held indirectly in the page's <c>/Properties</c> resource is read by
    /// the object scanner, so it decodes too. Only an inline literal string is affected.
    /// </para>
    /// </remarks>
    internal static class TextStringDecoder
    {
        /// <summary>
        /// Interpret <paramref name="token"/> as a text string, decoding it if the scanner that read
        /// it did not. Tokens that are already decoded are returned unchanged.
        /// </summary>
        public static string Decode(IDataToken<string> token)
        {
            // A literal string read with usePdfDocEncoding: false is the only undecoded case. A
            // HexToken has already applied the byte order mark rules, a StringToken carrying any
            // other encoding was decoded by the object scanner, and a NameToken is not a string.
            if (token is not StringToken { EncodedWith: StringToken.Encoding.Iso88591 } stringToken)
            {
                return token.Data;
            }

            var bytes = stringToken.GetBytes();

            // PDF 2.0 added UTF-8, marked by a byte order mark, as a text string encoding.
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
            }

            // Not a text string encoding the specification defines, but the string tokenizer accepts
            // it, so accept it here too rather than decode the same bytes two ways.
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            }

            // No byte order mark means PdfDocEncoding, which matches ASCII for 32 - 126.
            return PdfDocEncoding.TryConvertBytesToString(bytes, out var result) ? result! : token.Data;
        }
    }
}
