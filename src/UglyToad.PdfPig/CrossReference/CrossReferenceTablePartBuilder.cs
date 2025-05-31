namespace UglyToad.PdfPig.CrossReference
{
    using System.Collections.Generic;
    using Core;
    using Tokens;

    internal class CrossReferenceTablePartBuilder
    {
        private readonly Dictionary<IndirectReference, int> objects = new Dictionary<IndirectReference, int>();

        public int Offset { get; set; }

        public int Previous { get; set; }

        public DictionaryToken? Dictionary { get; set; }

        public CrossReferenceType XRefType { get; set; }

        public int? TiedToPreviousAtOffset { get; set; }

        public void Add(int objectId, int generationNumber, int offset)
        {
            IndirectReference objKey = new IndirectReference(objectId, generationNumber);

            if (!objects.ContainsKey(objKey))
            {
                objects[objKey] = offset;
            }
        }

        public CrossReferenceTablePart Build()
        {
            return new CrossReferenceTablePart(objects, Offset, Previous, Dictionary!, XRefType, TiedToPreviousAtOffset);
        }
    }
}