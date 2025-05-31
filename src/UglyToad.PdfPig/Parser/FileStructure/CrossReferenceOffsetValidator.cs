namespace UglyToad.PdfPig.Parser.FileStructure
{
    using Core;
    using Tokenization.Scanner;

    internal class CrossReferenceOffsetValidator
    {
        private readonly XrefOffsetValidator offsetValidator;

        public CrossReferenceOffsetValidator(XrefOffsetValidator offsetValidator)
        {
            this.offsetValidator = offsetValidator;
        }

        public int Validate(int crossReferenceOffset, ISeekableTokenScanner scanner, IInputBytes bytes, bool isLenientParsing)
        {
            int fixedOffset = offsetValidator.CheckXRefOffset(crossReferenceOffset, scanner, bytes, isLenientParsing);
            if (fixedOffset > -1)
            {
                crossReferenceOffset = fixedOffset;
            }

            return crossReferenceOffset;
        }
    }
}
