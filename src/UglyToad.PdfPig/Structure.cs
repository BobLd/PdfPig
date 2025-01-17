namespace UglyToad.PdfPig
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Core;
    using CrossReference;
    using Parser.Parts;
    using System.Linq;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Provides access to explore and retrieve the underlying PDF objects from the document.
    /// </summary>
    public class Structure
    {
        /// <summary>
        /// The root of the document's hierarchy providing access to the page tree as well as other information.
        /// </summary>
        public Catalog Catalog { get; }
        
        /// <summary>
        /// The cross-reference table enables direct access to objects by number.
        /// </summary>
        public CrossReferenceTable CrossReferenceTable { get; }

        /// <summary>
        /// Provides access to tokenization capabilities for objects by object number.
        /// </summary>
        internal IPdfTokenScanner TokenScanner { get; }

        internal Structure(Catalog catalog, CrossReferenceTable crossReferenceTable,
            IPdfTokenScanner scanner)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            CrossReferenceTable = crossReferenceTable ?? throw new ArgumentNullException(nameof(crossReferenceTable));
            TokenScanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        }

        /// <summary>
        /// Retrieve the tokenized object with the specified object reference number.
        /// </summary>
        /// <param name="reference">The object reference number.</param>
        /// <returns>The tokenized PDF object from the file.</returns>
        public ObjectToken GetObject(IndirectReference reference)
        {
            return TokenScanner.Get(reference) ?? throw new InvalidOperationException($"Could not find the object with reference: {reference}.");
        }

        private static void Build(ArrayToken arrayToken, IPdfTokenScanner tokenScanner, string? label, OptionalContentGroupElement ocgs)
        {
            ocgs.NestedRaw ??= new List<OptionalContentGroupElement>();
            string? currentLabel = label; // Default

            bool isPreviousTokenDico = false;
            foreach (var order in arrayToken.Data)
            {
                if (order is NameToken nameToken)
                {
                    currentLabel = nameToken.Data;
                    isPreviousTokenDico = false;
                }
                else if (order is StringToken stringToken)
                {
                    currentLabel = stringToken.Data;
                    isPreviousTokenDico = false;
                }
                else if (order is HexToken hexToken)
                {
                    currentLabel = hexToken.Data;
                    isPreviousTokenDico = false;
                }
                else if (order is ArrayToken nesterArrayToken)
                {
                    Build(nesterArrayToken, tokenScanner, currentLabel, isPreviousTokenDico ? ocgs.NestedRaw.Last() : ocgs);
                    isPreviousTokenDico = false;
                }
                else if (DirectObjectFinder.TryGet(order, tokenScanner, out DictionaryToken v))
                {
                    ocgs.NestedRaw.Add(new OptionalContentGroupElement(v, tokenScanner, currentLabel));
                    isPreviousTokenDico = true;
                }
            }
        }

        public bool TryGetOptionalContentProperties(out IReadOnlyList<OptionalContentGroupElement>? o)
        {
            o = null;
            if (!Catalog.CatalogDictionary.TryGet(NameToken.Ocproperties, TokenScanner, out DictionaryToken ocDictionaryToken))
            {
                return false;
            }

            if (!ocDictionaryToken.TryGet(NameToken.Ocgs, TokenScanner, out ArrayToken ocgsArray))
            {
                // Required
                return false;
            }

            if (!ocDictionaryToken.TryGet(NameToken.D, TokenScanner, out DictionaryToken viewDictionary))
            {
                // Required
                return false;
            }

            var root = new OptionalContentGroupElement();
            if (viewDictionary.TryGet(NameToken.Order, TokenScanner, out ArrayToken orderArray))
            {
                Build(orderArray, TokenScanner, null, root);

            }

            if (ocDictionaryToken.TryGet(NameToken.Create("Configs"), TokenScanner, out ArrayToken configsArray))
            {
                // Optional
            }

            o = root.NestedRaw;

            return true;
        }
    }
}
