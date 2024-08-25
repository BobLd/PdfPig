namespace UglyToad.PdfPig.Filters
{
    using System.Collections.Generic;
    using System.Linq;
    using Tokens;
    using UglyToad.PdfPig.Filters.Jbig2;

    internal sealed class Jbig2DecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported => true;

        /// <inheritdoc />
        public ReadOnlyMemory<byte> Decode(ReadOnlySpan<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            var decodeParams = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);
            Jbig2Document globalDocument = null;
            if (decodeParams.TryGet(NameToken.Jbig2Globals, out StreamToken tok))
            {
                globalDocument = new Jbig2Document(new ImageInputStream(tok.Data.ToArray()));
            }

            using (var jbig2 = new Jbig2Document(new ImageInputStream(input.ToArray()),
                globalDocument != null ? globalDocument.GetGlobalSegments() : null))
            {
                var page = jbig2.GetPage(1);
                var bitmap = page.GetBitmap();

                var pageInfo = (PageInformation)page.GetPageInformationSegment().GetSegmentData();

                globalDocument?.Dispose();

                if (pageInfo.DefaultPixelValue != 0 || IsImageMask(streamDictionary))
                {
                    // Invert bits if the default pixel value is black
                    return bitmap.GetByteArray().Select(x => (byte)~x).ToArray();
                }

                return bitmap.GetByteArray();
            }
        }

        private static bool IsImageMask(DictionaryToken streamDictionary)
        {
            if (streamDictionary.TryGet(NameToken.ImageMask, out BooleanToken isMaskToken) && isMaskToken != null)
            {
                return isMaskToken.Data;
            }

            if (streamDictionary.TryGet(NameToken.Im, out BooleanToken isMaskToken2) && isMaskToken2 != null)
            {
                return isMaskToken2.Data;
            }

            return false;
        }
    }
}