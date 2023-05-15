namespace UglyToad.PdfPig
{
    using Logging;
    using System.Collections.Generic;

    /// <summary>
    /// <see cref="ParsingOptions"/> but without being a public API/
    /// </summary>
    public class InternalParsingOptions
    {
        /// <summary>
        /// TODO
        /// </summary>
        public IReadOnlyList<string> Passwords { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public bool UseLenientParsing { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public bool ClipPaths { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public bool SkipMissingFonts { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public bool SkipMissingXObjects { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public ILog Logger { get; }

        internal InternalParsingOptions(
            IReadOnlyList<string> passwords,
            bool useLenientParsing,
            bool clipPaths,
            bool skipMissingFonts,
            bool skipMissingXObjects,
            ILog logger)
        {
            Passwords = passwords;
            UseLenientParsing = useLenientParsing;
            ClipPaths = clipPaths;
            SkipMissingFonts = skipMissingFonts;
            SkipMissingXObjects = skipMissingXObjects;
            Logger = logger;
        }
    }
}