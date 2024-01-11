namespace UglyToad.PdfPig.Filters
{
    // TODO - Check https://releases.aspose.com/words/java/release-notes/2021/aspose-words-for-java-21-12-release-notes/

    using CSJ2K;
    using CSJ2K.j2k.util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tokens;

    internal class JpxDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            var image = J2kImage.FromBytes(input.ToArray());
            return image.GetBytes();
        }
    }
}