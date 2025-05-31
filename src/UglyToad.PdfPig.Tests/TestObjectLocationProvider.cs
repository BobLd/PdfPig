namespace UglyToad.PdfPig.Tests
{
    using PdfPig.Core;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;

    internal class TestObjectLocationProvider : IObjectLocationProvider
    {
        public Dictionary<IndirectReference, int> Offsets { get; } = new Dictionary<IndirectReference, int>();

        public bool TryGetOffset(IndirectReference reference, out int offset)
        {
            return Offsets.TryGetValue(reference, out offset);
        }

        public void UpdateOffset(IndirectReference reference, int offset)
        {
            Offsets[reference] = offset;
        }

        public bool TryGetCached(IndirectReference reference, out ObjectToken objectToken)
        {
            objectToken = null;
            return false;
        }

        public void Cache(ObjectToken objectToken, bool force = false)
        {
        }
    }
}