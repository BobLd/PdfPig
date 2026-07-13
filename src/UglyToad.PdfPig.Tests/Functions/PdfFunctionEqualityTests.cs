namespace UglyToad.PdfPig.Tests.Functions
{
    using System.Text;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Tests.Tokens;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.Util;

    public class PdfFunctionEqualityTests
    {
        private static ArrayToken Arr(params double[] values)
        {
            return new ArrayToken(values.Select(v => (IToken)new NumericToken(v)).ToArray());
        }

        private static DictionaryToken Type2Dictionary(double n)
        {
            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(2) },
                { NameToken.Domain, Arr(0, 1) },
                { NameToken.C0, Arr(0) },
                { NameToken.C1, Arr(1) },
                { NameToken.N, new NumericToken(n) }
            });
        }

        private static PdfFunctionType2 CreateType2(double n = 1.0, double[]? c0 = null, double[]? c1 = null, double[]? range = null)
        {
            return new PdfFunctionType2(
                Type2Dictionary(n),
                Arr(0, 1),
                range is null ? null : Arr(range),
                Arr(c0 ?? [0.0]),
                Arr(c1 ?? [1.0]),
                n);
        }

        private static PdfFunctionType0 CreateType0(int bitsPerSample = 8, int order = 1, double[]? encode = null)
        {
            var dict = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(0) },
                { NameToken.Domain, Arr(0, 1) },
                { NameToken.Range, Arr(0, 1) },
                { NameToken.BitsPerSample, new NumericToken(bitsPerSample) },
                { NameToken.Size, Arr(2) }
            });

            return new PdfFunctionType0(dict, Arr(0, 1), Arr(0, 1), Arr(2), bitsPerSample, order, Arr(encode ?? [0.0, 1.0]), Arr(0, 1));
        }

        private static PdfFunctionType3 CreateType3(double bound = 0.5, double n1 = 1.0, double n2 = 2.0)
        {
            var dict = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(3) },
                { NameToken.Domain, Arr(0, 1) },
                { NameToken.Bounds, Arr(bound) },
                { NameToken.Encode, Arr(0, 1, 0, 1) }
            });

            return new PdfFunctionType3(
                dict,
                Arr(0, 1),
                null,
                new List<PdfFunction> { CreateType2(n1), CreateType2(n2) },
                Arr(bound),
                Arr(0, 1, 0, 1));
        }

        private static PdfFunctionType4 CreateType4(string program = "{ add }")
        {
            var dict = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(4) },
                { NameToken.Domain, Arr(-1, 1, -1, 1) },
                { NameToken.Range, Arr(-1, 1) }
            });

            var stream = new StreamToken(dict, Encoding.ASCII.GetBytes(program));

            var func = PdfFunctionParser.Create(stream, new TestPdfTokenScanner(), new TestFilterProvider());
            return Assert.IsType<PdfFunctionType4>(func);
        }

        [Fact]
        public void Type2EqualsAndGetHashCode()
        {
            var f1 = CreateType2();
            var f2 = CreateType2();
            var differentN = CreateType2(n: 2.0);
            var differentC0 = CreateType2(c0: [0.5]);

            Assert.Equal(f1, f2);
            Assert.Equal(f2, f1);
            Assert.Equal(f1.GetHashCode(), f2.GetHashCode());
            Assert.NotEqual<PdfFunction>(f1, differentN);
            Assert.NotEqual<PdfFunction>(f1, differentC0);
        }

        [Fact]
        public void Type2EqualsNullAndOtherTypes()
        {
            var f = CreateType2();

            Assert.False(f.Equals(null));
            Assert.False(f.Equals((object?)null));
            Assert.False(f.Equals(new object()));
            Assert.False(EqualityComparer<PdfFunction>.Default.Equals(f, null));
            Assert.False(EqualityComparer<PdfFunction>.Default.Equals(null, f));
        }

        [Fact]
        public void EqualsWhenBothRangeValuesAreNull()
        {
            var f1 = CreateType2(range: null);
            var f2 = CreateType2(range: null);

            Assert.Equal<PdfFunction>(f1, f2);
            Assert.Equal<PdfFunction>(f2, f1);
            Assert.Equal(f1.GetHashCode(), f2.GetHashCode());
        }

        [Fact]
        public void EqualsWhenBothRangeValuesAreSet()
        {
            var f1 = CreateType2(range: [0.0, 1.0]);
            var f2 = CreateType2(range: [0.0, 1.0]);

            Assert.Equal<PdfFunction>(f1, f2);
            Assert.Equal<PdfFunction>(f2, f1);
            Assert.Equal(f1.GetHashCode(), f2.GetHashCode());
        }

        [Fact]
        public void EqualsIsSymmetricWhenOnlyOneHasRange()
        {
            // Both functions share the same dictionary so only the Range values differ.
            var withRange = CreateType2(range: [0.0, 1.0]);
            var withoutRange = CreateType2(range: null);

            Assert.False(withRange.Equals(withoutRange));
            Assert.False(withoutRange.Equals(withRange));
        }

        [Fact]
        public void Type0EqualsAndGetHashCode()
        {
            var f1 = CreateType0();
            var f2 = CreateType0();
            var differentBits = CreateType0(bitsPerSample: 16);
            var differentEncode = CreateType0(encode: [0.0, 0.5]);

            Assert.Equal<PdfFunction>(f1, f2);
            Assert.Equal(f1.GetHashCode(), f2.GetHashCode());
            Assert.NotEqual<PdfFunction>(f1, differentBits);
            Assert.NotEqual<PdfFunction>(f1, differentEncode);
            Assert.False(f1.Equals(null));
        }

        [Fact]
        public void Type3EqualsAndGetHashCode()
        {
            var f1 = CreateType3();
            var f2 = CreateType3();
            var differentBounds = CreateType3(bound: 0.25);
            var differentSubFunction = CreateType3(n2: 3.0);

            Assert.Equal<PdfFunction>(f1, f2);
            Assert.Equal(f1.GetHashCode(), f2.GetHashCode());
            Assert.NotEqual<PdfFunction>(f1, differentBounds);
            Assert.NotEqual<PdfFunction>(f1, differentSubFunction);
            Assert.False(f1.Equals(null));
        }

        [Fact]
        public void Type4EqualsAndGetHashCode()
        {
            var f1 = CreateType4();
            var f2 = CreateType4();
            var differentProgram = CreateType4("{ mul }");

            Assert.Equal<PdfFunction>(f1, f2);
            Assert.Equal(f1.GetHashCode(), f2.GetHashCode());
            Assert.NotEqual<PdfFunction>(f1, differentProgram);
            Assert.False(f1.Equals(null));
        }

        [Fact]
        public void DifferentFunctionTypesAreNotEqual()
        {
            var type0 = CreateType0();
            var type2 = CreateType2();
            var type3 = CreateType3();
            var type4 = CreateType4();

            PdfFunction[] functions = [type0, type2, type3, type4];

            for (var i = 0; i < functions.Length; i++)
            {
                for (var j = 0; j < functions.Length; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    Assert.False(
                        functions[i].Equals(functions[j]),
                        $"{functions[i].GetType().Name} should not equal {functions[j].GetType().Name}.");
                }
            }
        }

        [Fact]
        public void FunctionsCanBeUsedAsHashSetElements()
        {
            var set = new HashSet<PdfFunction> { CreateType2(), CreateType3() };

            Assert.Contains(CreateType2(), set);
            Assert.Contains(CreateType3(), set);
            Assert.DoesNotContain(CreateType2(n: 5.0), set);
        }
    }
}
