namespace UglyToad.PdfPig.Content
{
    using Graphics.Colors;
    using PdfFonts;
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// TODO
    /// </summary>
    public interface IResourceStore
    {
        /// <summary>
        /// TODO
        /// </summary>
        void LoadResourceDictionary(DictionaryToken resourceDictionary, InternalParsingOptions parsingOptions);

        /// <summary>
        /// Remove any named resources and associated state for the last resource dictionary loaded.
        /// Does not affect the cached resources, just the labels associated with them.
        /// </summary>
        void UnloadResourceDictionary();

        /// <summary>
        /// TODO
        /// </summary>
        IFont GetFont(NameToken name);

        /// <summary>
        /// TODO
        /// </summary>
        bool TryGetXObject(NameToken name, out StreamToken stream);

        /// <summary>
        /// TODO
        /// </summary>
        DictionaryToken GetExtendedGraphicsStateDictionary(NameToken name);

        /// <summary>
        /// TODO
        /// </summary>
        IFont GetFontDirectly(IndirectReferenceToken fontReferenceToken);

        /// <summary>
        /// TODO
        /// </summary>
        bool TryGetNamedColorSpace(NameToken name, out ResourceColorSpace namedColorSpace);

        /// <summary>
        /// TODO
        /// </summary>
        ColorSpaceDetails GetColorSpaceDetails(NameToken name, DictionaryToken dictionary);

        /// <summary>
        /// TODO
        /// </summary>
        DictionaryToken GetMarkedContentPropertiesDictionary(NameToken name);

        /// <summary>
        /// TODO
        /// </summary>
        IReadOnlyDictionary<NameToken, PatternColor> GetPatterns();

        /// <summary>
        /// TODO
        /// </summary>
        Shading GetShading(NameToken name);
    }
}