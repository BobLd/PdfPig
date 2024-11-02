namespace UglyToad.PdfPig.Content
{
    /// <summary>
    /// Linearisation of PDF is an optional feature available beginning in PDF 1.2
    /// that enables efficient incremental access of the file in a network environment.
    /// </summary>
    internal sealed class LinearisationParameter
    {
        /// <summary>
        /// A version identification for the linearised format.
        /// </summary>
        public double Version { get; }

        /// <summary>
        /// The length of the entire PDF file in bytes.
        /// <para>
        /// It shall be exactly equal to the actual length of the PDF file.
        /// A mismatch indicates that the file is not linearised and shall be treated as ordinary PDF file,
        /// ignoring linearisation information. (If the mismatch resulted from appending an update, the
        /// linearisation information may still be correct but requires validation).
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
        /// The page number of the first page (starts at 1).
        /// <para>
        /// Default value: 1
        /// </para>
        /// </summary>
        public int? FirstPageNumber { get; }

        public LinearisationParameter(double version, long size, long[] hintOffsets, long firstPageObjectNumber,
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
