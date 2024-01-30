namespace UglyToad.PdfPig.Filters
{
    using System;
    using Tokens;

    internal class JpxDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = false;

        /// <inheritdoc />
        public Span<byte> Decode(Span<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            throw new NotSupportedException("The JPX Filter (JPEG2000) for image data is not currently supported. " +
                                            "Try accessing the raw compressed data directly.");
        }
    }
}