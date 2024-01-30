﻿namespace UglyToad.PdfPig.Filters
{
    using System;
    using Tokens;

    internal class DctDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = false;

        /// <inheritdoc />
        public Span<byte> Decode(Span<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            throw new NotSupportedException("The DST (Discrete Cosine Transform) Filter indicates data is encoded in JPEG format. " +
                                            "This filter is not currently supported but the raw data can be supplied to JPEG supporting libraries.");
        }
    }
}
