namespace UglyToad.PdfPig.Filters
{
    using SkiaSharp;
    using System;
    using Tokens;

    internal sealed class JpxDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public ReadOnlyMemory<byte> Decode(ReadOnlySpan<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            byte[] image = input.ToArray();
            using (OpenJpegDotNet.IO.Reader reader = new OpenJpegDotNet.IO.Reader(image))
            {
                bool result = reader.ReadHeader();
                using (var i = reader.Decode())
                using (var raw = i.ToRawBitmap())
                {
                    return raw.Bytes;
                }
            }              
        }
    }
}