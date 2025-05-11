namespace UglyToad.PdfPig.XObjects
{
    using System;
    using System.Linq;
    using Content;
    using Core;
    using Filters;
    using Graphics;
    using Graphics.Colors;
    using Graphics.Core;
    using Images;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    /// <summary>
    /// External Object (XObject) factory.
    /// </summary>
    public static class XObjectFactory
    {
        /// <summary>
        /// Read the XObject image.
        /// </summary>
        public static XObjectImage ReadImage(XObjectContentRecord xObject,
            IPdfTokenScanner pdfScanner,
            ILookupFilterProvider filterProvider,
            IResourceStore resourceStore)
        {
            if (xObject is null)
            {
                throw new ArgumentNullException(nameof(xObject));
            }

            if (xObject.Type != XObjectType.Image)
            {
                throw new InvalidOperationException($"Cannot create an image from an XObject with type: {xObject.Type}.");
            }

            var dictionary = xObject.Stream.StreamDictionary.Resolve(pdfScanner);

            var bounds = xObject.AppliedTransformation.Transform(new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(1, 1)));

            var width = dictionary.GetInt(NameToken.Width);
            var height = dictionary.GetInt(NameToken.Height);

            var isImageMask = dictionary.TryGet(NameToken.ImageMask, out BooleanToken isMaskToken) && isMaskToken.Data;

            var decode = Array.Empty<double>();
            if (dictionary.TryGet(NameToken.Decode, out ArrayToken decodeArrayToken))
            {
                decode = decodeArrayToken.Data.OfType<NumericToken>()
                    .Select(x => x.Double)
                    .ToArray();
            }
            
            XObjectImage? softMaskImage = null;
            if (dictionary.TryGet(NameToken.Smask, pdfScanner, out StreamToken? sMaskToken))
            {
                softMaskImage = GetSoftMaskImage(sMaskToken, pdfScanner, filterProvider);
            }
            else if (dictionary.TryGet(NameToken.Mask, out StreamToken maskStream))
            {
                if (maskStream.StreamDictionary.TryGet(NameToken.ColorSpace, out NameToken softMaskColorSpace))
                {
                    throw new Exception("The SMask dictionary contains a 'ColorSpace'.");
                }

                // Stencil masking
                XObjectContentRecord maskImageRecord = new XObjectContentRecord(XObjectType.Image,
                    maskStream,
                    TransformationMatrix.Identity,
                    xObject.DefaultRenderingIntent,
                    null);

                softMaskImage = ReadImage(maskImageRecord, pdfScanner, filterProvider, resourceStore);
            }

            var isJpxDecode = dictionary.TryGet(NameToken.Filter, out NameToken filterName) && filterName.Equals(NameToken.JpxDecode);

            int bitsPerComponent = GetBitsPerComponent(xObject.Stream, isImageMask, isJpxDecode);

            var intent = xObject.DefaultRenderingIntent;
            if (dictionary.TryGet(NameToken.Intent, out NameToken renderingIntentToken))
            {
                intent = renderingIntentToken.Data.ToRenderingIntent();
            }

            var interpolate = dictionary.TryGet(NameToken.Interpolate, out BooleanToken? interpolateToken)
                              && interpolateToken.Data;

            var supportsFilters = true;
            var filters = filterProvider.GetFilters(dictionary, pdfScanner);
            foreach (var filter in filters)
            {
                if (!filter.IsSupported)
                {
                    supportsFilters = false;
                    break;
                }
            }

            var streamToken = new StreamToken(dictionary, xObject.Stream.Data);

            var decodedBytes = supportsFilters ? new Lazy<ReadOnlyMemory<byte>>(() => streamToken.Decode(filterProvider, pdfScanner))
                : null;
            
            ColorSpaceDetails? details = null;
            if (!isImageMask)
            {
                if (dictionary.TryGet(NameToken.ColorSpace, out NameToken? colorSpaceNameToken))
                {
                    details = resourceStore.GetColorSpaceDetails(colorSpaceNameToken, dictionary);
                }
                else if (dictionary.TryGet(NameToken.ColorSpace, out ArrayToken? colorSpaceArrayToken)
                    && colorSpaceArrayToken.Length > 0 && colorSpaceArrayToken.Data[0] is NameToken firstColorSpaceName)
                {
                    details = resourceStore.GetColorSpaceDetails(firstColorSpaceName, dictionary);
                }
                else if (!isJpxDecode)
                {
                    details = xObject.DefaultColorSpace;
                }
            }
            else
            {
                details = resourceStore.GetColorSpaceDetails(null, dictionary);
            }

            return new XObjectImage(
                bounds,
                width,
                height,
                bitsPerComponent,
                isJpxDecode,
                isImageMask,
                intent,
                interpolate,
                decode,
                dictionary,
                xObject.Stream.Data,
                decodedBytes,
                details,
                softMaskImage);
        }

        private static XObjectImage GetSoftMaskImage(StreamToken softMaskStreamToken,
            IPdfTokenScanner pdfScanner,
            ILookupFilterProvider filterProvider)
        {
            if (!softMaskStreamToken.StreamDictionary.TryGet(NameToken.Subtype, out NameToken softMaskSubType) || !softMaskSubType.Equals(NameToken.Image))
            {
                throw new Exception("The SMask dictionary does not contain a 'Subtype' entry, or its value is not 'Image'.");
            }

            if (!softMaskStreamToken.StreamDictionary.TryGet(NameToken.ColorSpace, out NameToken softMaskColorSpace) || !softMaskColorSpace.Equals(NameToken.Devicegray))
            {
                throw new Exception("The SMask dictionary does not contain a 'ColorSpace' entry, or its value is not 'Devicegray'.");
            }

            if (softMaskStreamToken.StreamDictionary.ContainsKey(NameToken.Mask) || softMaskStreamToken.StreamDictionary.ContainsKey(NameToken.Smask))
            {
                throw new Exception("The SMask dictionary contains a 'Mask' or 'Smask' entry.");
            }

            var decodeMask = Array.Empty<double>();
            if (softMaskStreamToken.StreamDictionary.TryGet(NameToken.Decode, out ArrayToken decodeMaskArrayToken))
            {
                decodeMask = decodeMaskArrayToken.Data.OfType<NumericToken>()
                    .Select(x => x.Double)
                    .ToArray();
            }

            var isMaskJpxDecode = softMaskStreamToken.StreamDictionary.TryGet(NameToken.Filter, out NameToken maskFilterName) && maskFilterName.Equals(NameToken.JpxDecode);

            var maskSupportsFilters = true;
            var maskFilters = filterProvider.GetFilters(softMaskStreamToken.StreamDictionary, pdfScanner);
            foreach (var filter in maskFilters)
            {
                if (!filter.IsSupported)
                {
                    maskSupportsFilters = false;
                    break;
                }
            }

            var streamToken = new StreamToken(softMaskStreamToken.StreamDictionary, softMaskStreamToken.Data);

            var maskDecodedBytes = maskSupportsFilters ? new Lazy<ReadOnlyMemory<byte>>(() =>
            {
                var memory = streamToken.Decode(filterProvider, pdfScanner);
                return memory;
                /*
                Memory<byte> scaled = new byte[memory.Length];

                for (int i = 0; i < memory.Length; ++i)
                {
                    scaled.Span[i] = Convert.ToByte(memory.Span[i] / 255.0);
                }

                return scaled;
                */
            }) : null;

            return new XObjectImage(
                new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(1, 1)),
                softMaskStreamToken.StreamDictionary.GetInt(NameToken.Width),
                softMaskStreamToken.StreamDictionary.GetInt(NameToken.Height),
                GetBitsPerComponent(softMaskStreamToken, false, isMaskJpxDecode),
                isMaskJpxDecode,
                false,
                RenderingIntent.RelativeColorimetric, // Ignored
                softMaskStreamToken.StreamDictionary.TryGet(NameToken.Interpolate, out BooleanToken? maskInterpolateToken) && maskInterpolateToken.Data,
                decodeMask,
                softMaskStreamToken.StreamDictionary,
                softMaskStreamToken.Data,
                maskDecodedBytes,
                DeviceGrayColorSpaceDetails.Instance,
                null);
        }
        
        private static int GetBitsPerComponent(StreamToken streamToken, bool isImageMask, bool isJpxDecode)
        {
            if (isImageMask)
            {
                return 1;
            }
            
            if (isJpxDecode)
            {
                // Optional for JPX
                if (streamToken.StreamDictionary.TryGet(NameToken.BitsPerComponent, out NumericToken? bitsPerComponentToken))
                {
                    return bitsPerComponentToken.Int;
                }
                
                return Jpeg2000Helper.GetBitsPerComponent(streamToken.Data.Span);
            }
            else
            {
                if (!streamToken.StreamDictionary.TryGet(NameToken.BitsPerComponent, out NumericToken? bitsPerComponentToken))
                {
                    throw new PdfDocumentFormatException($"No bits per component defined for image: {streamToken.StreamDictionary}.");
                }

                return bitsPerComponentToken.Int;
            }
        }
    }
}
