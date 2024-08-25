namespace UglyToad.PdfPig.Filters
{
    using System.Linq;
    using Tokens;
    using UglyToad.PdfPig.Filters.Jbig2;

    internal sealed class Jbig2DecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public ReadOnlyMemory<byte> Decode(ReadOnlySpan<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            var decodeParms = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);
            Jbig2Document globalDocument = null;
            if (decodeParms.TryGet(NameToken.Jbig2Globals, out StreamToken tok))
            {
                globalDocument = new Jbig2Document(new ImageInputStream(tok.Data.ToArray()));
            }

            using (var jbig2 = new Jbig2Document(new ImageInputStream(input.ToArray()),
                       globalDocument != null ? globalDocument.GetGlobalSegments() : null))
            {
                var page = jbig2.GetPage(1);
                var bitmap = page.GetBitmap();

                var pageInfo =
                    (PageInformation)page.GetPageInformationSegment().GetSegmentData();

                globalDocument?.Dispose();
                
                if (pageInfo.DefaultPixelValue == 0 && !IsImageMask(streamDictionary))
                {
                    return bitmap.GetByteArray();
                }

                var data = bitmap.GetByteArray();

                // Invert bits if the default pixel value is black
                for (int i = 0; i < data.Length; ++i)
                {
                    ref byte x = ref data[i];
                    x = (byte)~x;
                }

                //return bitmap.GetByteArray().Select(x => (byte)~x).ToArray();

                return data;
            }
        }

        private static bool IsImageMask(DictionaryToken streamDictionary)
        {
            if (streamDictionary.TryGet(NameToken.ImageMask, out BooleanToken isImageMask))
            {
                return isImageMask.Data;
            }

            if (streamDictionary.TryGet(NameToken.Im, out BooleanToken isIm))
            {
                return isIm.Data;
            }

            return false;
        }
    }
}