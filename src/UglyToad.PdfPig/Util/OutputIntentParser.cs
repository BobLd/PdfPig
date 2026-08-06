namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using Filters;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Graphics.Colors.Icc;
    
    /// <summary>
    /// Resolves the document's output intent ICC profile from the catalog's
    /// <c>/OutputIntents</c> array. PDF/X files characterize their device colour
    /// (DeviceCMYK / DeviceRGB / DeviceGray) through the output intent's
    /// <c>/DestOutputProfile</c>; rendering those device colours through that profile
    /// (rather than a fixed approximation) is what keeps colour-managed content and
    /// device-colour content visually consistent.
    /// </summary>
    internal static class OutputIntentParser
    {
        /// <summary>
        /// Try to resolve and parse the most usable output intent from the
        /// <c>/OutputIntents</c> array of the given dictionary. This works for both the document
        /// catalog and a page object (PDF 2.0, Table 31), which may each carry <c>/OutputIntents</c>.
        /// <para>
        /// Where several entries are present, one embedding a usable <c>/DestOutputProfile</c> always wins;
        /// among equally usable entries the <c>/S</c> subtype decides, preferring <c>GTS_PDFX</c>, then
        /// <c>GTS_PDFA1</c>, then array order.
        /// </para>
        /// </summary>
        public static OutputIntent? Create(DictionaryToken dictionary, IPdfTokenScanner scanner,
            ILookupFilterProvider filterProvider, IIccProfileService? iccProfileService)
        {
            if (iccProfileService is null)
            {
                return null;
            }

            if (!dictionary.TryGet(NameToken.OutputIntents, scanner, out ArrayToken? outputIntents))
            {
                return null;
            }

            // A document may carry several output intents (PDF 2.0, 14.11.5), so array order alone is not a
            // sound way to pick one: a file with both a PDF/A and a PDF/X intent would otherwise get whichever
            // the producer happened to write first. Rank by the /S subtype instead and prefer GTS_PDFX, because
            // characterizing device colour for the target press is precisely the PDF/X semantic this feature
            // implements; GTS_PDFA1 comes next, then anything else (ISO_PDFE1, extension subtypes) in array
            // order. Ranking happens before any profile is touched, so decoding stays lazy - a DestOutputProfile
            // is routinely megabytes and only the entries actually considered are decoded.
            var ranked = new List<(int Rank, int Index, DictionaryToken Dictionary)>(outputIntents.Data.Count);

            for (int i = 0; i < outputIntents.Data.Count; i++)
            {
                if (!DirectObjectFinder.TryGet(outputIntents.Data[i], scanner, out DictionaryToken? entryDictionary))
                {
                    continue;
                }

                ranked.Add((GetSubtypeRank(entryDictionary, scanner), i, entryDictionary));
            }

            ranked.Sort(static (a, b) => a.Rank != b.Rank ? a.Rank.CompareTo(b.Rank) : a.Index.CompareTo(b.Index));

            // An output intent that embeds a usable DestOutputProfile is preferred over the subtype ranking,
            // because only that can drive colour management: a GTS_PDFA1 entry carrying a profile beats a
            // GTS_PDFX entry that only references one. The first such entry is returned immediately. Otherwise
            // the best-ranked parsable entry is kept as a fallback so a reference-only output intent
            // (DestOutputProfileRef, PDF 2.0) and its metadata are still surfaced rather than silently dropped.
            OutputIntent? fallback = null;

            foreach (var (_, _, intentDictionary) in ranked)
            {
                string name = "";
                if (intentDictionary.TryGet(NameToken.S, scanner, out NameToken? nameToken))
                {
                    name = nameToken.Data;
                }

                string? outputCondition = null;
                if (intentDictionary.TryGet(NameToken.OutputCondition, scanner, out StringToken? outputConditionToken))
                {
                    outputCondition = outputConditionToken?.Data;
                }

                string outputConditionIdentifier = "";
                if (intentDictionary.TryGet(NameToken.OutputConditionIdentifier, scanner, out StringToken? outputConditionIdentifierToken))
                {
                    outputConditionIdentifier = outputConditionIdentifierToken.Data;
                }

                string registryName = "";
                if (intentDictionary.TryGet(NameToken.RegistryName, scanner, out StringToken? registryNameToken))
                {
                    registryName = registryNameToken.Data;
                }

                string? info = null;
                if (intentDictionary.TryGet(NameToken.Info, scanner, out StringToken? infoToken))
                {
                    info = infoToken?.Data;
                }

                IccProfileReference? destOutputProfileRef = null;
                if (intentDictionary.TryGet(NameToken.DestOutputProfileRef, scanner, out DictionaryToken? refDictionary))
                {
                    destOutputProfileRef = ParseProfileReference(refDictionary, scanner);
                }

                intentDictionary.TryGet(NameToken.MixingHints, scanner, out DictionaryToken? mixingHints);
                intentDictionary.TryGet(NameToken.SpectralData, scanner, out DictionaryToken? spectralData);

                // The embedded profile is optional and parsed leniently: a missing or unreadable
                // DestOutputProfile leaves the colour-management transform null but must not abort
                // resolution of the remaining entries.
                IIccProfile? profile = TryParseDestOutputProfile(intentDictionary, scanner, filterProvider, iccProfileService);

                var outputIntent = new OutputIntent(name, outputCondition, outputConditionIdentifier, registryName, info,
                    profile, destOutputProfileRef, mixingHints, spectralData);

                if (profile is not null)
                {
                    return outputIntent;
                }

                fallback ??= outputIntent;
            }

            return fallback;
        }

        private static int GetSubtypeRank(DictionaryToken intentDictionary, IPdfTokenScanner scanner)
        {
            if (!intentDictionary.TryGet(NameToken.S, scanner, out NameToken? subtype))
            {
                return 2; // Other
            }

            return subtype.Data switch // Lower is better
            {
                "GTS_PDFX" => 0,
                "GTS_PDFA1" => 1,
                _ => 2 // Other
            };
        }

        private static IIccProfile? TryParseDestOutputProfile(DictionaryToken intentDictionary,
            IPdfTokenScanner scanner, ILookupFilterProvider filterProvider, IIccProfileService iccProfileService)
        {
            if (!intentDictionary.TryGet(NameToken.DestOutputProfile, scanner, out StreamToken? profileStream))
            {
                return null;
            }

            Memory<byte> bytes;
            try
            {
                bytes = profileStream.Decode(filterProvider, scanner);
            }
            catch
            {
                return null;
            }

            return iccProfileService.TryGetProfile(bytes, out var profile) ? profile : null;
        }

        private static IccProfileReference ParseProfileReference(DictionaryToken refDictionary, IPdfTokenScanner scanner)
        {
            string? profileCS = null;
            if (refDictionary.TryGet(NameToken.ProfileCS, scanner, out StringToken? profileCsString))
            {
                profileCS = profileCsString.Data;
            }
            else if (refDictionary.TryGet(NameToken.ProfileCS, scanner, out NameToken? profileCsName))
            {
                profileCS = profileCsName.Data;
            }

            string? profileName = null;
            if (refDictionary.TryGet(NameToken.ProfileName, scanner, out StringToken? profileNameString))
            {
                profileName = profileNameString.Data;
            }

            byte[]? iccVersion = null;
            if (refDictionary.TryGet(NameToken.IccVersion, scanner, out StringToken? iccVersionString))
            {
                iccVersion = iccVersionString.GetBytes();
            }

            byte[]? checkSum = null;
            if (refDictionary.TryGet(NameToken.CheckSum, scanner, out StringToken? checkSumString))
            {
                checkSum = checkSumString.GetBytes();
            }

            refDictionary.TryGet(NameToken.ColorantTable, scanner, out DictionaryToken? colorantTable);
            refDictionary.TryGet(NameToken.Urls, scanner, out ArrayToken? urls);

            return new IccProfileReference(profileCS, profileName, iccVersion, checkSum, colorantTable, urls);
        }
    }
}
