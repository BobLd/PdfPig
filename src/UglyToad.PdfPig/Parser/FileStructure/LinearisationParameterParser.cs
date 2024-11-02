namespace UglyToad.PdfPig.Parser.FileStructure
{
    using System.Linq;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Content;

    internal static class LinearisationParameterParser
    {
        public static LinearisationParameter? Parse(IPdfTokenScanner scanner, ParsingOptions parsingOptions)
        {
            /*
             * Following the header, the first object in the body of the PDF file (part 2) shall be
             * an indirect dictionary object, the linearization parameter dictionary, which shall
             * contain the parameters listed in "Table F.1 — Entries in the linearization parameter
             * dictionary". All values in this dictionary shall be direct objects. There shall be no
             * references to this dictionary anywhere in the document; however, the first- page
             * cross-reference table (part 3) shall contain a normal entry for it.
             *
             * The linearization parameter dictionary shall be entirely contained within the first
             * 1024 bytes of the PDF file. This limits the amount of data a PDF processor will have
             * to read before deciding whether the file is linearized
             */

            // F.3.3 Linearization parameter dictionary (Part 2)

            if (!scanner.MoveNext())
            {
                scanner.Seek(0);
                return null;
            }

            if (scanner.CurrentPosition > 1024)
            {
                // TODO - Assert and check data is actually absent

                scanner.Seek(0);
                return null;
            }

            var objToken = scanner.CurrentToken as ObjectToken;

            if (objToken?.Data is DictionaryToken dictionaryToken &&
                dictionaryToken.TryGet(NameToken.Linearized, scanner, out NumericToken? linearisedFormat))
            {
                // (Required) The length of the entire PDF file in bytes. It shall be exactly equal to
                // the actual length of the PDF file. A mismatch indicates that the file is not linearized
                // and shall be treated as ordinary PDF file, ignoring linearization information. (If the
                // mismatch resulted from appending an update, the linearization information may still be
                // correct but requires validation; see G.7, "Accessing an updated file" for details.)
                if (!dictionaryToken.TryGet(NameToken.L, scanner, out NumericToken? documentLengthToken))
                {
                    // integer
                    // Error - required
                    scanner.Seek(0);
                    return null;
                }

                // (Required) An array of two or four integers, [offset1 length1] or [offset1 length1 offset2 length2].
                // offset1 shall be the offset of the primary hint stream from the beginning of the PDF file.
                // (This is the beginning of the stream object, not the beginning of the stream data.) length1
                // shall be the length of this stream, including stream object overhead.
                //
                // If the value of the primary hint stream dictionary’s Length entry is an indirect reference, the
                // object it refers to shall immediately follow the stream object, and length1 also shall include
                // the length of the indirect length object, including object overhead.
                //
                // If there is an overflow hint stream, offset2 and length2 shall specify its offset and length.
                if (!dictionaryToken.TryGet(NameToken.H, scanner, out ArrayToken? offsetArray))
                {
                    // integer
                    scanner.Seek(0);
                    // Error - required
                    return null;
                }

                // (Required) The object number of the first page’s page object.
                if (!dictionaryToken.TryGet(NameToken.O, scanner, out NumericToken? firstPageToken))
                {
                    // integer
                    // Error - required
                    scanner.Seek(0);
                    return null;
                }

                // (Required) The offset of the end of the first page (the end of Example 6 in F.3,
                // "Linearized PDF document structure"), relative to the beginning of the PDF file.
                if (!dictionaryToken.TryGet(NameToken.E, scanner, out NumericToken? firstPageEndOffsetToken))
                {
                    // integer
                    // Error - required
                    scanner.Seek(0);
                    return null;
                }

                // (Required) The number of pages in the document.
                if (!dictionaryToken.TryGet(NameToken.N, scanner, out NumericToken? numberOfPageToken))
                {
                    // integer
                    // Error - required
                    scanner.Seek(0);
                    return null;
                }

                // (Required) In documents that use standard main cross-reference tables (including
                // hybrid-reference files; see 7.5.8.4, "Compatibility with applications that do not
                // support compressed reference streams"), this entry shall represent the offset of
                // the white-space character preceding the first entry of the main cross-reference table
                // (the entry for object number 0), relative to the beginning of the PDF file. Note that
                // this differs from the Prev entry in the first-page trailer, which gives the location
                // of the xref line that precedes the table.
                if (!dictionaryToken.TryGet(NameToken.T, scanner, out NumericToken? mainCrossReferenceOffsetToken))
                {
                    // integer
                    // Error - required
                    scanner.Seek(0);
                    return null;
                }

                // (Optional) The page number of the first page; see F.3.4, "First-page cross-reference
                // table and trailer (Part 3)". Default value: 0.
                int firstPageNumber = 0;
                if (dictionaryToken.TryGet(NameToken.P, scanner, out NumericToken? firstPageNumberToken))
                {
                    firstPageNumber = firstPageNumberToken.Int;
                }

                var linearisation = new LinearisationParameter(linearisedFormat.Double,
                    documentLengthToken.Long,
                    offsetArray.Data.OfType<NumericToken>().Select(t => t.Long).ToArray(),
                    firstPageToken.Long,
                    firstPageEndOffsetToken.Long,
                    numberOfPageToken.Int,
                    mainCrossReferenceOffsetToken.Long,
                    firstPageNumber + 1);

                scanner.Seek(0);

                return linearisation;
            }

            scanner.Seek(0);
            return null;
        }
    }
}
