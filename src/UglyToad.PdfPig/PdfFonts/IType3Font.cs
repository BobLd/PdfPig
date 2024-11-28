namespace UglyToad.PdfPig.PdfFonts
{
    using UglyToad.PdfPig.Graphics;

    /// <summary>
    /// Type 3 font interface.
    /// </summary>
    public interface IType3Font : IFont
    {
        /// <summary>
        /// Try render the glyph.
        /// </summary>
        /// <param name="characterCode"></param>
        /// <param name="operationContext"></param>
        /// <returns></returns>
        bool TryRender(int characterCode, IOperationContext operationContext);
    }
}
