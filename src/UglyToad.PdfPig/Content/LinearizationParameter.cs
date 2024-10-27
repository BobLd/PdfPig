namespace UglyToad.PdfPig.Content
{
    using System.Runtime.CompilerServices;

    internal sealed class LinearizationParameter
    {
        /// <summary>
        /// A version identification for the linearized format.
        /// </summary>
        public double Version { get; }

        /// <summary>
        /// The length of the entire PDF file in bytes.
        /// <para>
        /// It shall be exactly equal to the actual length of the PDF file. A mismatch indicates that the file is not linearized and shall be treated as ordinary PDF file, ignoring linearization information. (If the mismatch resulted from appending an update, the linearization information may still be correct but requires validation).
        /// </para>
        /// </summary>
        public long Size { get; }

        public long[] HintOffsets { get; }

        public long FirstPageObjectNumber { get; }

        public long FirstPageEndOffset { get; }

        /// <summary>
        /// The number of pages in the document.
        /// </summary>
        public int NumberOfPages { get; }

        public long MainCrossReferenceOffset { get; }

        /// <summary>
        /// The page number of the first page.
        /// <para>
        /// Default value: 0
        /// </para>
        /// </summary>
        public int? FirstPageNumber { get; }

        public LinearizationParameter(double version, long size, long[] hintOffsets, long firstPageObjectNumber,
            long firstPageEndOffset, int numberOfPages, long mainCrossReferenceOffset, int? firstPageNumber)
        {
            Version = version;
            Size = size;
            HintOffsets = hintOffsets;
            FirstPageObjectNumber = firstPageObjectNumber;
            FirstPageEndOffset = firstPageEndOffset;
            NumberOfPages = numberOfPages;
            MainCrossReferenceOffset = mainCrossReferenceOffset;
            FirstPageNumber = firstPageNumber;
        }
    }
}
