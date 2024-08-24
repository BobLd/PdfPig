namespace UglyToad.PdfPig.PdfFonts
{
    using Tokens;

    internal interface IFontFactory
    {
        IFont Get(string key, DictionaryToken dictionary);
    }
}