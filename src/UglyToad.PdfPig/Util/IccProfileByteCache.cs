namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Filters;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Document-scoped cache of decoded ICC profile streams.
    /// <para>
    /// An <c>/ICCBased</c> colour space is re-parsed every time its resource dictionary is loaded, because
    /// <c>ResourceStore.LoadResourceDictionary</c> clears the colour space caches (colour space <i>names</i>
    /// are scoped to the resource dictionary, so those caches cannot survive the switch). Without this cache
    /// that means re-inflating the whole profile stream once per page and once per Form XObject with its own
    /// <c>/Resources</c>. Embedded CMYK profiles are routinely over a megabyte, and a viewer that re-renders
    /// a page on every zoom step pays the cost each time.
    /// </para>
    /// <para>
    /// Nothing survives between those loads to key on: the <see cref="StreamToken"/>, its stream dictionary
    /// and even the backing byte array are all freshly allocated on each resolution. So there are two keys:
    /// </para>
    /// <list type="number">
    /// <item>the profile's indirect reference, when the colour space array still holds one - an exact,
    /// document-wide identity, and the common case; and</item>
    /// <item>otherwise the raw (still encoded) stream bytes, compared for equality. A colour space reached
    /// as the base or alternate of an Indexed/Separation/DeviceN space arrives with its stream already
    /// resolved to a direct token, so this fallback is what covers image colour spaces.</item>
    /// </list>
    /// <para>
    /// The content comparison is a vectorised <see cref="MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>
    /// over the <i>compressed</i> bytes, which is far cheaper than the inflate it avoids, and a document has
    /// only a handful of distinct profiles, so the linear scan stays short. It is bounded regardless by
    /// <see cref="MaxContentEntries"/>.
    /// </para>
    /// </summary>
    internal sealed class IccProfileByteCache
    {
        /// <summary>
        /// Upper bound on content-keyed entries, so a pathological document cannot grow the scan or the
        /// retained bytes without limit. Past this point profiles are decoded uncached.
        /// </summary>
        private const int MaxContentEntries = 32;

        private readonly Dictionary<IndirectReference, ReadOnlyMemory<byte>> byReference = new();

        private readonly List<(byte[] Raw, ReadOnlyMemory<byte> Decoded)> byContent = new();

        /// <summary>
        /// Decode the profile stream, reusing a previously decoded copy of the same profile when there is
        /// one. Returns <see cref="ReadOnlyMemory{T}.Empty"/> when the stream cannot be decoded, in which
        /// case the caller falls back to the colour space's alternate.
        /// </summary>
        /// <param name="profileToken">
        /// The token the stream was resolved from, used as the cache key when it is an indirect reference.
        /// </param>
        /// <param name="profileStream">The resolved profile stream.</param>
        public ReadOnlyMemory<byte> GetOrDecode(IToken profileToken, StreamToken profileStream,
            ILookupFilterProvider filterProvider, IPdfTokenScanner scanner)
        {
            if (profileToken is IndirectReferenceToken reference)
            {
                if (byReference.TryGetValue(reference.Data, out var cached))
                {
                    return cached;
                }

                // A failed decode is cached as empty on purpose: retrying a corrupt multi-megabyte stream
                // on every page is exactly the cost this cache exists to avoid.
                var decoded = Decode(profileStream, filterProvider, scanner);
                byReference[reference.Data] = decoded;
                return decoded;
            }

            ReadOnlySpan<byte> raw = profileStream.Data.Span;

            foreach (var entry in byContent)
            {
                if (entry.Raw.Length == raw.Length && raw.SequenceEqual(entry.Raw))
                {
                    return entry.Decoded;
                }
            }

            var decodedByContent = Decode(profileStream, filterProvider, scanner);

            if (byContent.Count < MaxContentEntries)
            {
                byContent.Add((raw.ToArray(), decodedByContent));
            }

            return decodedByContent;
        }

        private static ReadOnlyMemory<byte> Decode(StreamToken profileStream,
            ILookupFilterProvider filterProvider, IPdfTokenScanner scanner)
        {
            try
            {
                return profileStream.Decode(filterProvider, scanner);
            }
            catch
            {
                return ReadOnlyMemory<byte>.Empty;
            }
        }
    }
}
