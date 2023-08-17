namespace UglyToad.PdfPig.PdfFonts
{
    using Tokens;

    /// <summary>
    /// Font factory interface.
    /// </summary>
    public interface IFontFactory
    {
        /// <summary>
        /// Get the font.
        /// </summary>
        IFont Get(DictionaryToken dictionary);
    }
}