namespace UglyToad.PdfPig.Functions
{
    using System;
    using System.Linq;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Exponential interpolation function
    /// </summary>
    internal sealed class PdfFunctionType2 : PdfFunction
    {
        /// <summary>
        /// Exponential interpolation function
        /// </summary>
        internal PdfFunctionType2(DictionaryToken function, ArrayToken domain, ArrayToken? range, ArrayToken c0, ArrayToken c1, double n)
            : base(function, domain, range)
        {
            C0 = c0.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            C1 = c1.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            N = n;
        }

        internal PdfFunctionType2(StreamToken function, ArrayToken domain, ArrayToken? range, ArrayToken c0, ArrayToken c1, double n)
            : base(function, domain, range)
        {
            C0 = c0.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            C1 = c1.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            N = n;
        }

        public override FunctionTypes FunctionType => FunctionTypes.Exponential;

        public override Span<double> Eval(Span<double> input)
        {
            // exponential interpolation
            double xToN = Math.Pow(input[0], N); // x^exponent

            var result = new double[Math.Min(C0.Length, C1.Length)];
            for (int j = 0; j < result.Length; j++)
            {
                double c0j = C0[j];
                double c1j = C1[j];
                result[j] = c0j + xToN * (c1j - c0j);
            }

            return ClipToRange(result);
        }

        /// <summary>
        /// The C0 values of the function, 0 if empty.
        /// </summary>
        public double[] C0 { get; }

        /// <summary>
        /// The C1 values of the function, 1 if empty.
        /// </summary>
        public double[] C1 { get; }

        /// <summary>
        /// The exponent of the function.
        /// </summary>
        public double N { get; }

        public override string ToString()
        {
            return "FunctionType2{"
                + "C0: " + C0 + " "
                + "C1: " + C1 + " "
                + "N: " + N + "}";
        }
    }
}
