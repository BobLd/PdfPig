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
        public static XObjectImage ReadImage(XObjectContentRecord xObject, IPdfTokenScanner pdfScanner,
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

            var dictionary = xObject.Stream.StreamDictionary.Resolve(pdfScanner); // Resolve all indirect refs

            var bounds = xObject.AppliedTransformation.Transform(new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(1, 1)));

            var width = dictionary.Get<NumericToken>(NameToken.Width, pdfScanner).Int; // No need for pdfScanner anymore
            var height = dictionary.Get<NumericToken>(NameToken.Height, pdfScanner).Int; // No need for pdfScanner anymore

            var isImageMask = dictionary.TryGet(NameToken.ImageMask, pdfScanner, out BooleanToken? isMaskToken)  // No need for pdfScanner anymore
                         && isMaskToken.Data;

            var isJpxDecode = dictionary.TryGet(NameToken.Filter, out var token)
                && token is NameToken filterName
                && filterName.Equals(NameToken.JpxDecode);

            int bitsPerComponent = 0;
            if (!isImageMask && !isJpxDecode)
            {
                if (!dictionary.TryGet(NameToken.BitsPerComponent, pdfScanner, out NumericToken? bitsPerComponentToken))  // No need for pdfScanner anymore
                {
                    throw new PdfDocumentFormatException($"No bits per component defined for image: {dictionary}.");
                }

                bitsPerComponent = bitsPerComponentToken.Int;
            }
            else if (isImageMask)
            {
                bitsPerComponent = 1;
            }

            var intent = xObject.DefaultRenderingIntent;
            if (dictionary.TryGet(NameToken.Intent, out NameToken renderingIntentToken))
            {
                intent = renderingIntentToken.Data.ToRenderingIntent();
            }

            var interpolate = dictionary.TryGet(NameToken.Interpolate, pdfScanner, out BooleanToken? interpolateToken) // No need for pdfScanner anymore
                              && interpolateToken.Data;

            DictionaryToken? filterDictionary = xObject.Stream.StreamDictionary;
            if (xObject.Stream.StreamDictionary.TryGet(NameToken.Filter, out var filterToken)
                && filterToken is IndirectReferenceToken)
            {
                if (filterDictionary.TryGet(NameToken.Filter, pdfScanner, out ArrayToken? filterArray)) // No need for pdfScanner anymore
                {
                    filterDictionary = filterDictionary.With(NameToken.Filter, filterArray);
                }
                else if (filterDictionary.TryGet(NameToken.Filter, pdfScanner, out NameToken? filterNameToken)) // No need for pdfScanner anymore
                {
                    filterDictionary = filterDictionary.With(NameToken.Filter, filterNameToken);
                }
                else
                {
                    filterDictionary = null;
                }
            }

            var supportsFilters = filterDictionary != null;
            if (filterDictionary != null)
            {
                var filters = filterProvider.GetFilters(filterDictionary, pdfScanner); // No need for pdfScanner anymore
                foreach (var filter in filters)
                {
                    if (!filter.IsSupported)
                    {
                        supportsFilters = false;
                        break;
                    }
                }
            }

            var decodeParams = dictionary.GetObjectOrDefault(NameToken.DecodeParms, NameToken.Dp);
            if (decodeParams is IndirectReferenceToken refToken)
            {
                dictionary = dictionary.With(NameToken.DecodeParms, pdfScanner.Get(refToken.Data).Data); // No need for pdfScanner anymore
            }

            var streamToken = new StreamToken(dictionary, xObject.Stream.Data);

            var decodedBytes = supportsFilters ? new Lazy<ReadOnlyMemory<byte>>(() => streamToken.Decode(filterProvider, pdfScanner)) // No need for pdfScanner anymore
                : null;

            var decode = Array.Empty<double>();

            if (dictionary.TryGet(NameToken.Decode, pdfScanner, out ArrayToken? decodeArrayToken)) // No need for pdfScanner anymore
            {
                decode = decodeArrayToken.Data.OfType<NumericToken>()
                    .Select(x => x.Double)
                    .ToArray();
            }

            ColorSpaceDetails? details = null;
            if (!isImageMask)
            {
                if (dictionary.TryGet(NameToken.ColorSpace, pdfScanner, out NameToken? colorSpaceNameToken)) // No need for pdfScanner anymore
                {
                    details = resourceStore.GetColorSpaceDetails(colorSpaceNameToken, dictionary);
                }
                else if (dictionary.TryGet(NameToken.ColorSpace, pdfScanner, out ArrayToken? colorSpaceArrayToken) // No need for pdfScanner anymore
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
                details);
        }
    }
}
