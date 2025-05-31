namespace UglyToad.PdfPig.CrossReference
{
    using System.Collections.Generic;
    using Core;
    using Tokens;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// The format of an in-use entry is
    ///     nnnnnnnnnn ggggg n eol
    /// where
    ///     nnnnnnnnnn is a 10-digit byte offset
    ///     ggggg is a 5-digit generation number
    ///     n is a literal keyword identifying this as an in-use entry
    ///     eol is a 2-character end-of-line sequence
    ///
    ///
    /// The byte offset is a 10-digit number, padded with leading zeros if necessary,
    /// giving the number of bytes from the beginning of the file to the beginning of the
    /// object.
    /// </remarks>
    internal class CrossReferenceTablePart
    {
        public IReadOnlyDictionary<IndirectReference, int> ObjectOffsets { get; }

        public int Offset { get; private set; }

        public int Previous { get; }

        public DictionaryToken Dictionary { get; private set; }

        public CrossReferenceType Type { get; }

        /// <summary>
        /// For Xref streams indicated by tables they should be used together when constructing the final table.
        /// </summary>
        public long? TiedToXrefAtOffset { get; }

        public CrossReferenceTablePart(
            IReadOnlyDictionary<IndirectReference, int> objectOffsets,
            int offset, int previous,
            DictionaryToken dictionary,
            CrossReferenceType type,
            int? tiedToXrefAtOffset)
        {
            ObjectOffsets = objectOffsets;
            Offset = offset;
            Previous = previous;
            Dictionary = dictionary;
            Type = type;
            TiedToXrefAtOffset = tiedToXrefAtOffset;
        }

        public void FixOffset(int offset)
        {
            Offset = offset;
            Dictionary = Dictionary.With(NameToken.Prev, new NumericToken((double)offset));
        }

        public int GetPreviousOffset()
        {
            if (Dictionary.TryGet(NameToken.Prev, out var token) && token is NumericToken numeric)
            {
                return numeric.Int;
            }

            return -1;
        }
    }
}