namespace UglyToad.PdfPig.PdfFonts.Parser.Handlers
{
    using Tokens;

    internal interface IFontHandler
    {
        IFont Generate(string key, DictionaryToken dictionary);
    }
}