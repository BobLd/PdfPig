namespace UglyToad.PdfPig.Filters
{
    using JpegLibrary;
    using JpegLibrary.Utils;
    using System;
    using Tokens;

    // based on https://github.com/yigolden/JpegLibrary/blob/main/apps/JpegDecode/DecodeAction.cs
    internal class DctDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public ReadOnlyMemory<byte> Decode(ReadOnlySpan<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            var decoder = new JpegDecoder();
            decoder.SetInput(input.ToArray());
            decoder.Identify();
            
            int width = decoder.Width;
            int height = decoder.Height;

            Memory<byte> ycbcr = new byte[width * height * decoder.NumberOfComponents];

            if (decoder.Precision == 8)
            {
                // This is the most common case for JPEG.
                // We use the fatest implement.
                decoder.SetOutputWriter(
                    new JpegBufferOutputWriter8Bit(width, height, decoder.NumberOfComponents, ycbcr));
            }
            else if (decoder.Precision < 8)
            {
                //decoder.SetOutputWriter(new JpegBufferOutputWriterLessThan8Bit(width, height, decoder.Precision, 3, ycbcr));
            }
            else
            {
                //decoder.SetOutputWriter(new JpegBufferOutputWriterGreaterThan8Bit(width, height, decoder.Precision, 3, ycbcr));
            }

            decoder.Decode();

            bool shouldTransform = false;
            if (decoder.AdobeApplicationSpecific.HasValue)
            {
                /*
                 * If the encoding algorithm has inserted the Adobe-defined marker code in the encoded data indicating the
                 * ColorTransform value, then the colours shall be transformed, or not, after the DCT decoding has been
                 * performed according to the value provided in the encoded data and the value of this dictionary entry
                 * shall be ignored.
                 */

                /*
                 * See 'SERIES T: TERMINALS FOR TELEMATIC SERVICES, Still-image compression – JPEG-1 extensions', Section 6.5.3
                 * Transform flag values of 0, 1 and 2 shall be supported and are interpreted as follows:
                 *  0 – CMYK for images that are encoded with four components in which all four CMYK values are
                 *      complemented; RGB for images that are encoded with three components; i.e., the APP14 marker does not
                 *      specify a transform applied to the image data.
                 *  1 – An image encoded with three components using YCbCr colour encoding.
                 *  2 – An image encoded with four components using YCCK colour encoding.
                 */
                shouldTransform = decoder.AdobeApplicationSpecific.Value.ColorTransformCode > 0;
            }
            else if (streamDictionary.TryGet(NameToken.Create("ColorTransform"), out var token))
            {
                /*
                 * If the Adobe-defined marker code in the encoded data indicating the ColorTransform value is not present
                 * then the value specified in this dictionary entry will be used.
                 */

                // TODO - use scanner? Need to make sure it's a direct ref

            }
            else
            {
                // GHOSTSCRIPT-699178-0.pdf
                // GHOSTSCRIPT-700370-2.pdf
                // GHOSTSCRIPT-700139-0.pdf

                /*
                 * If the Adobe-defined marker code (APP14) in the encoded data indicating the ColorTransform value is not
                 * present and this dictionary entry is not present in the filter dictionary then the default value of
                 * ColorTransform shall be 1 if the image has three components and 0 otherwise.
                 */

                shouldTransform = decoder.NumberOfComponents == 3;
            }

            if (!shouldTransform)
            {
                return ycbcr;
            }

            if (decoder.NumberOfComponents == 3)
            {
                // Convert YCbCr to RGB
                for (int i = 0; i < height; i++)
                {
                    JpegColorConverter.Shared.ConvertYCbCr8ToRgb24(ycbcr.Span.Slice(i * width * 3, width * 3),
                        ycbcr.Span.Slice(i * width * 3, width * 3),
                        width);
                }
            }
            else if (decoder.NumberOfComponents == 4)
            {
                // GHOSTSCRIPT-695241-0.pdf
                // GHOSTSCRIPT-697234-0.pdf
                // GHOSTSCRIPT-698721-0.zip-6.pdf

                // Convert YCbCrK to CMYK
                for (int i = 0; i < height; i++)
                {
                    JpegColorConverter.Shared.ConvertYCbCrK8ToCmyk24(ycbcr.Span.Slice(i * width * 4, width * 4),
                        ycbcr.Span.Slice(i * width * 4, width * 4),
                        width);
                }
            }

            return ycbcr;
        }
    }
}
