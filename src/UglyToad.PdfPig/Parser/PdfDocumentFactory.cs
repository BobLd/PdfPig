namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using AcroForms;
    using Content;
    using Core;
    using CrossReference;
    using Encryption;
    using FileStructure;
    using Filters;
    using Fonts.SystemFonts;
    using Graphics;
    using Outline;
    using Parts;
    using Parts.CrossReference;
    using PdfFonts;
    using PdfFonts.Parser;
    using PdfFonts.Parser.Handlers;
    using PdfFonts.Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal static class PdfDocumentFactory
    {
        public static PdfDocument Open(byte[] fileBytes, ParsingOptions? options = null)
        {
            var inputBytes = new MemoryInputBytes(fileBytes);

            return Open(inputBytes, options);
        }

        public static PdfDocument Open(string filename, ParsingOptions? options = null)
        {
            if (!File.Exists(filename))
            {
                throw new InvalidOperationException("No file exists at: " + filename);
            }

            return Open(File.ReadAllBytes(filename), options);
        }

        internal static PdfDocument Open(Stream stream, ParsingOptions? options)
        {
            var initialPosition = stream.Position;

            var streamInput = new StreamInputBytes(stream, false);

            try
            {
                return Open(streamInput, options);
            }
            catch (Exception ex)
            {
                if (initialPosition != 0)
                {
                    throw new InvalidOperationException(
                        "Could not parse document due to an error, the input stream was not at position zero when provided to the Open method.",
                        ex);
                }

                throw;
            }
        }

        private static PdfDocument Open(IInputBytes inputBytes, ParsingOptions? options = null)
        {
            options ??= new ParsingOptions()
            {
                UseLenientParsing = true,
                ClipPaths = false,
                SkipMissingFonts = false
            };

            var tokenScanner = new CoreTokenScanner(inputBytes, true, useLenientParsing: options.UseLenientParsing);

            var passwords = new List<string>();

            if (options.Password != null)
            {
                passwords.Add(options.Password);
            }

            if (options.Passwords != null)
            {
                passwords.AddRange(options.Passwords.Where(x => x != null));
            }

            if (!passwords.Contains(string.Empty))
            {
                passwords.Add(string.Empty);
            }

            options.Passwords = passwords;

            var document = OpenDocument(inputBytes, tokenScanner, options);

            return document;
        }

        private static PdfDocument OpenDocument(
            IInputBytes inputBytes,
            ISeekableTokenScanner scanner,
            ParsingOptions parsingOptions)
        {
            var filterProvider = new FilterProviderWithLookup(parsingOptions.FilterProvider ?? DefaultFilterProvider.Instance);

            CrossReferenceTable? crossReferenceTable = null;

            var xrefValidator = new XrefOffsetValidator(parsingOptions.Logger);

            // We're ok with this since our intent is to lazily load the cross reference table.
            // ReSharper disable once AccessToModifiedClosure
            var locationProvider = new ObjectLocationProvider(() => crossReferenceTable, inputBytes);
            var pdfScanner = new PdfTokenScanner(inputBytes, locationProvider, filterProvider, NoOpEncryptionHandler.Instance, parsingOptions);

            var version = FileHeaderParser.Parse(scanner, inputBytes, parsingOptions.UseLenientParsing, parsingOptions.Logger);

            // F.3.3 Linearization parameter dictionary (Part 2)
            LinearizationParameter? linearization = null;
            if (pdfScanner.MoveNext())
            {
                var objToken = pdfScanner.CurrentToken as ObjectToken;
                if (objToken?.Data is DictionaryToken dictionaryToken &&
                    dictionaryToken.TryGet(NameToken.Linearized, pdfScanner, out NumericToken? linearisedFormat))
                {
                    // TODO

                    // (Required) The length of the entire PDF file in bytes. It shall be exactly equal to
                    // the actual length of the PDF file. A mismatch indicates that the file is not linearized
                    // and shall be treated as ordinary PDF file, ignoring linearization information. (If the
                    // mismatch resulted from appending an update, the linearization information may still be
                    // correct but requires validation; see G.7, "Accessing an updated file" for details.)
                    if (!dictionaryToken.TryGet(NameToken.L, pdfScanner, out NumericToken? documentLengthToken))
                    {
                        // integer
                        // Error - required
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
                    if (!dictionaryToken.TryGet(NameToken.H, pdfScanner, out ArrayToken? offsetArray))
                    {
                        // integer
                        // Error - required
                    }

                    // (Required) The object number of the first page’s page object.
                    if (!dictionaryToken.TryGet(NameToken.O, pdfScanner, out NumericToken? firstPageToken))
                    {
                        // integer
                        // Error - required
                    }

                    // (Required) The offset of the end of the first page (the end of Example 6 in F.3,
                    // "Linearized PDF document structure"), relative to the beginning of the PDF file.
                    if (!dictionaryToken.TryGet(NameToken.E, pdfScanner, out NumericToken? firstPageEndOffsetToken))
                    {
                        // integer
                        // Error - required
                    }

                    // (Required) The number of pages in the document.
                    if (!dictionaryToken.TryGet(NameToken.N, pdfScanner, out NumericToken? numberOfPageToken))
                    {
                        // integer
                        // Error - required
                    }

                    // (Required) In documents that use standard main cross-reference tables (including
                    // hybrid-reference files; see 7.5.8.4, "Compatibility with applications that do not
                    // support compressed reference streams"), this entry shall represent the offset of
                    // the white-space character preceding the first entry of the main cross-reference table
                    // (the entry for object number 0), relative to the beginning of the PDF file. Note that
                    // this differs from the Prev entry in the first-page trailer, which gives the location
                    // of the xref line that precedes the table.
                    if (!dictionaryToken.TryGet(NameToken.T, pdfScanner, out NumericToken? mainCrossReferenceOffsetToken))
                    {
                        // integer
                        // Error - required
                    }

                    // (Optional) The page number of the first page; see F.3.4, "First-page cross-reference
                    // table and trailer (Part 3)". Default value: 0.
                    int firstPageNumber = 0;
                    if (dictionaryToken.TryGet(NameToken.P, pdfScanner, out NumericToken? firstPageNumberToken))
                    {
                        firstPageNumber = firstPageNumberToken.Int;
                    }

                    linearization = new LinearizationParameter(linearisedFormat.Double,
                        documentLengthToken.Long,
                        offsetArray.Data.OfType<NumericToken>().Select(t => t.Long).ToArray(),
                        firstPageToken.Long,
                        firstPageEndOffsetToken.Long,
                        numberOfPageToken.Int,
                        mainCrossReferenceOffsetToken.Long,
                        firstPageNumber + 1);
                }
                else
                {
                    pdfScanner.Seek(0);
                }
            }

            var crossReferenceStreamParser = new CrossReferenceStreamParser(filterProvider);
            var crossReferenceParser = new CrossReferenceParser(parsingOptions.Logger, xrefValidator, crossReferenceStreamParser);
            
            var crossReferenceOffset = FileTrailerParser.GetFirstCrossReferenceOffset(
                inputBytes,
                scanner,
                parsingOptions.UseLenientParsing) + version.OffsetInFile;

            // TODO: make this use the scanner.
            var validator = new CrossReferenceOffsetValidator(xrefValidator);

            crossReferenceOffset = validator.Validate(crossReferenceOffset, scanner, inputBytes, parsingOptions.UseLenientParsing);

            crossReferenceTable = crossReferenceParser.Parse(
                inputBytes,
                parsingOptions.UseLenientParsing,
                crossReferenceOffset,
                version.OffsetInFile,
                pdfScanner,
                scanner);

            var (rootReference, rootDictionary) = ParseTrailer(
                crossReferenceTable,
                parsingOptions.UseLenientParsing,
                pdfScanner,
                out var encryptionDictionary);

            var encryptionHandler = encryptionDictionary != null ?
                (IEncryptionHandler)new EncryptionHandler(
                    encryptionDictionary,
                    crossReferenceTable.Trailer,
                    parsingOptions.Passwords)
                : NoOpEncryptionHandler.Instance;

            pdfScanner.UpdateEncryptionHandler(encryptionHandler);

            var cidFontFactory = new CidFontFactory(
                parsingOptions.Logger,
                pdfScanner,
                filterProvider);

            var encodingReader = new EncodingReader(pdfScanner);

            var type0Handler = new Type0FontHandler(
                cidFontFactory,
                filterProvider,
                pdfScanner,
                parsingOptions);

            var type1Handler = new Type1FontHandler(pdfScanner, filterProvider, encodingReader);

            var trueTypeHandler = new TrueTypeFontHandler(parsingOptions.Logger,
                pdfScanner,
                filterProvider,
                encodingReader,
                SystemFontFinder.Instance,
                type1Handler);

            var fontFactory = new FontFactory(
                parsingOptions.Logger,
                type0Handler,
                trueTypeHandler,
                type1Handler,
                new Type3FontHandler(pdfScanner, filterProvider, encodingReader));

            var resourceContainer = new ResourceStore(pdfScanner, fontFactory, filterProvider, parsingOptions);

            var information = DocumentInformationFactory.Create(
                pdfScanner,
                crossReferenceTable.Trailer,
                parsingOptions.UseLenientParsing);

            var pageFactory = new PageFactory(pdfScanner, resourceContainer, filterProvider,
                new PageContentParser(new ReflectionGraphicsStateOperationFactory(), parsingOptions.UseLenientParsing), parsingOptions);

            var catalog = CatalogFactory.Create(
                rootReference,
                rootDictionary,
                pdfScanner,
                pageFactory,
                parsingOptions.Logger,
                parsingOptions.UseLenientParsing);

            var acroFormFactory = new AcroFormFactory(pdfScanner, filterProvider, crossReferenceTable);
            var bookmarksProvider = new BookmarksProvider(parsingOptions.Logger, pdfScanner);

            return new PdfDocument(
                inputBytes,
                version,
                crossReferenceTable,
                catalog,
                information,
                encryptionDictionary,
                pdfScanner,
                filterProvider,
                acroFormFactory,
                bookmarksProvider,
                linearization,
                parsingOptions);
        }

        private static (IndirectReference, DictionaryToken) ParseTrailer(
            CrossReferenceTable crossReferenceTable,
            bool isLenientParsing,
            IPdfTokenScanner pdfTokenScanner,
            [NotNullWhen(true)] out EncryptionDictionary? encryptionDictionary)
        {
            encryptionDictionary = GetEncryptionDictionary(crossReferenceTable, pdfTokenScanner);

            var rootDictionary = DirectObjectFinder.Get<DictionaryToken>(crossReferenceTable.Trailer.Root, pdfTokenScanner)!;

            if (!rootDictionary.ContainsKey(NameToken.Type) && isLenientParsing)
            {
                rootDictionary = rootDictionary.With(NameToken.Type, NameToken.Catalog);
            }

            return (crossReferenceTable.Trailer.Root, rootDictionary);
        }

        private static EncryptionDictionary? GetEncryptionDictionary(CrossReferenceTable crossReferenceTable, IPdfTokenScanner pdfTokenScanner)
        {
            if (crossReferenceTable.Trailer.EncryptionToken is null)
            {
                return null;
            }

            if (!DirectObjectFinder.TryGet(crossReferenceTable.Trailer.EncryptionToken, pdfTokenScanner, out DictionaryToken? encryptionDictionaryToken))
            {
                if (DirectObjectFinder.TryGet(crossReferenceTable.Trailer.EncryptionToken, pdfTokenScanner, out NullToken? _))
                {
                    return null;
                }

                throw new PdfDocumentFormatException($"Unrecognized encryption token in trailer: {crossReferenceTable.Trailer.EncryptionToken}.");
            }

            var result = EncryptionDictionaryFactory.Read(encryptionDictionaryToken, pdfTokenScanner);

            return result;
        }
    }
}
