namespace UglyToad.PdfPig.Functions
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Tokens;

    internal sealed class PdfFunctionType0 : PdfFunction
    {
        /// <summary>
        /// The samples of the function.
        /// </summary>
        private int[][]? samples;

        /// <summary>
        /// Stitching function
        /// </summary>
        internal PdfFunctionType0(DictionaryToken function, ArrayToken domain, ArrayToken range, ArrayToken size, int bitsPerSample, int order, ArrayToken encode, ArrayToken decode)
            : base(function, domain, range)
        {
            BitsPerSample = bitsPerSample;
            Order = order;

            Size = size.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            EncodeValues = encode?.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            DecodeValues = decode?.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
        }

        /// <summary>
        /// Stitching function
        /// </summary>
        internal PdfFunctionType0(StreamToken function, ArrayToken domain, ArrayToken range, ArrayToken size, int bitsPerSample, int order, ArrayToken? encode, ArrayToken? decode)
            : base(function, domain, range)
        {
            BitsPerSample = bitsPerSample;
            Order = order;

            Size = size.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            EncodeValues = encode?.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            DecodeValues = decode?.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
        }

        public override FunctionTypes FunctionType => FunctionTypes.Sampled;

        /// <summary>
        /// The "Size" entry, which is the number of samples in each input dimension of the sample table.
        /// <para>An array of m positive integers specifying the number of samples in each input dimension of the sample table.</para>
        /// </summary>
        public double[] Size { get; }

        /// <summary>
        /// Get the number of bits that the output value will take up.
        /// <para>Valid values are 1,2,4,8,12,16,24,32.</para>
        /// </summary>
        /// <returns>Number of bits for each output value.</returns>
        public int BitsPerSample { get; }

        /// <summary>
        /// Get the order of interpolation between samples. Valid values are 1 and 3,
        /// specifying linear and cubic spline interpolation, respectively. Default
        /// is 1. See p.170 in PDF spec 1.7.
        /// </summary>
        /// <returns>order of interpolation.</returns>
        public int Order { get; }

        /// <summary>
        /// An array of 2 x m numbers specifying the linear mapping of input values 
        /// into the domain of the function's sample table. Default value: [ 0 (Size0
        /// - 1) 0 (Size1 - 1) ...].
        /// </summary>
        private double[]? EncodeValues { get; }

        /// <summary>
        /// An array of 2 x n numbers specifying the linear mapping of sample values
        /// into the range appropriate for the function's output values. Default
        /// value: same as the value of Range.
        /// </summary>
        private double[]? DecodeValues { get; }

        /// <summary>
        /// Get the encode for the input parameter.
        /// </summary>
        /// <param name="paramNum">The function parameter number.</param>
        /// <returns>The encode parameter range or null if none is set.</returns>
        public PdfRange? GetEncodeForParameter(int paramNum)
        {
            if (EncodeValues != null && EncodeValues.Length >= paramNum * 2 + 1)
            {
                return new PdfRange(EncodeValues, paramNum);
            }
            return null;
        }

        /// <summary>
        /// Get the decode for the input parameter.
        /// </summary>
        /// <param name="paramNum">The function parameter number.</param>
        /// <returns>The decode parameter range or null if none is set.</returns>
        public PdfRange? GetDecodeForParameter(int paramNum)
        {
            if (DecodeValues != null && DecodeValues.Length >= paramNum * 2 + 1)
            {
                return new PdfRange(DecodeValues, paramNum);
            }
            return null;
        }

        /// <summary>
        /// Inner class do to an interpolation in the Nth dimension by comparing the
        /// content size of N-1 dimensional objects.This is done with the help of
        /// recursive calls.
        /// <para>To understand the algorithm without recursion, see <see href="http://harmoniccode.blogspot.de/2011/04/bilinear-color-interpolation.html"/> for a bilinear interpolation
        /// and <see href="https://en.wikipedia.org/wiki/Trilinear_interpolation"/> for trilinear interpolation.
        /// </para>
        /// </summary>
        internal sealed class RInterpol
        {
            // coordinate of the "ceil" point
            private readonly int[] inPrev;
            // coordinate of the "floor" point
            private readonly int[] inNext;
            private readonly int numberOfInputValues;
            private readonly int numberOfOutputValues;
            private readonly double[] size;

            private readonly int[][] samples;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="inputPrev">coordinate of the "ceil" point</param>
            /// <param name="inputNext">coordinate of the "floor" point</param>
            /// <param name="numberOfInputValues"></param>
            /// <param name="numberOfOutputValues"></param>
            /// <param name="size"></param>
            /// <param name="samples"></param>
            internal RInterpol(int[] inputPrev, int[] inputNext, int numberOfInputValues, int numberOfOutputValues, double[] size, int[][] samples)
            {
                inPrev = inputPrev;
                inNext = inputNext;
                this.numberOfInputValues = numberOfInputValues;
                this.numberOfOutputValues = numberOfOutputValues;
                this.size = size;
                this.samples = samples;
            }

            /// <summary>
            /// Calculate the interpolation.
            /// </summary>
            /// <returns>interpolated result sample</returns>
            internal double[] RInterpolate(ReadOnlySpan<double> input)
            {
                return InternalRInterpol(input, new int[numberOfInputValues], 0);
            }

            /// <summary>
            /// Do a linear interpolation if the two coordinates can be known, or
            /// call itself recursively twice.
            /// </summary>
            /// <param name="input"></param>
            /// <param name="coord">partially set coordinate (not set from step
            /// upwards); gets fully filled in the last call ("leaf"), where it is
            /// used to get the correct sample</param>
            /// <param name="step">between 0 (first call) and dimension - 1</param>
            /// <returns>interpolated result sample</returns>
            private double[] InternalRInterpol(ReadOnlySpan<double> input, int[] coord, int step)
            {
                double[] resultSample = new double[numberOfOutputValues];
                if (step == input.Length - 1)
                {
                    // leaf
                    if (inPrev[step] == inNext[step])
                    {
                        coord[step] = inPrev[step];
                        int[] tmpSample = samples[CalcSampleIndex(coord)];
                        for (int i = 0; i < numberOfOutputValues; ++i)
                        {
                            resultSample[i] = tmpSample[i];
                        }
                        return resultSample;
                    }
                    coord[step] = inPrev[step];
                    int[] sample1 = samples[CalcSampleIndex(coord)];
                    coord[step] = inNext[step];
                    int[] sample2 = samples[CalcSampleIndex(coord)];
                    for (int i = 0; i < numberOfOutputValues; ++i)
                    {
                        resultSample[i] = Interpolate(input[step], inPrev[step], inNext[step], sample1[i], sample2[i]);
                    }
                    return resultSample;
                }
                else
                {
                    // branch
                    if (inPrev[step] == inNext[step])
                    {
                        coord[step] = inPrev[step];
                        return InternalRInterpol(input, coord, step + 1);
                    }
                    coord[step] = inPrev[step];
                    double[] sample1 = InternalRInterpol(input, coord, step + 1);
                    coord[step] = inNext[step];
                    double[] sample2 = InternalRInterpol(input, coord, step + 1);
                    for (int i = 0; i < numberOfOutputValues; ++i)
                    {
                        resultSample[i] = Interpolate(input[step], inPrev[step], inNext[step], sample1[i], sample2[i]);
                    }
                    return resultSample;
                }
            }

            /// <summary>
            /// calculate array index (structure described in p.171 PDF spec 1.7) in multiple dimensions.
            /// </summary>
            /// <param name="vector">with coordinates</param>
            /// <returns>index in flat array</returns>
            private int CalcSampleIndex(ReadOnlySpan<int> vector)
            {
                // inspiration: http://stackoverflow.com/a/12113479/535646
                // but used in reverse
                int index = 0;
                int sizeProduct = 1;
                int dimension = vector.Length;

                for (int i = dimension - 2; i >= 0; --i)
                {
                    sizeProduct = (int)(sizeProduct * size[i]);
                }

                for (int i = dimension - 1; i >= 0; --i)
                {
                    index += sizeProduct * vector[i];
                    if (i - 1 >= 0)
                    {
                        sizeProduct = (int)(sizeProduct / size[i - 1]);
                    }
                }
                return index;
            }
        }

        /// <summary>
        /// Get all sample values of this function.
        /// </summary>
        /// <returns>an array with all samples.</returns>
        private int[][] GetSamples()
        {
            if (samples is null)
            {
                int arraySize = 1;
                int nIn = NumberOfInputParameters;
                int nOut = NumberOfOutputParameters;
                for (int i = 0; i < nIn; i++)
                {
                    arraySize *= (int)Size[i];
                }
                samples = new int[arraySize][];
                int bitsPerSample = BitsPerSample;

                // PDF spec 1.7 p.171:
                // Each sample value is represented as a sequence of BitsPerSample bits. 
                // Successive values are adjacent in the bit stream; there is no padding at byte boundaries.
                var bits = new BitArray(FunctionStream!.Data.ToArray());

                for (int i = 0; i < arraySize; i++)
                {
                    samples[i] = new int[nOut];
                    for (int k = 0; k < nOut; k++)
                    {
                        long accum = 0L;
                        for (int l = bitsPerSample - 1; l >= 0; l--)
                        {
                            accum <<= 1;
                            accum |= bits[i * nOut * bitsPerSample + (k * bitsPerSample) + l] ? (uint)1 : 0;
                        }

                        // TODO will this cast work properly for 32 bitsPerSample or should we use long[]?
                        samples[i][k] = (int)accum;
                    }
                }
            }

            return samples;
        }

        public override Span<double> Eval(Span<double> input)
        {
            //This involves linear interpolation based on a set of sample points.
            //Theoretically it's not that difficult ... see section 3.9.1 of the PDF Reference.

            int bitsPerSample = BitsPerSample;
            double maxSample = Math.Pow(2, bitsPerSample) - 1.0;
            int numberOfInputValues = input.Length;
            int numberOfOutputValues = NumberOfOutputParameters;

            int[] inputPrev = new int[numberOfInputValues];
            int[] inputNext = new int[numberOfInputValues];
            //input = input.ToArray(); // PDFBOX-4461 // TODO - Not sure if required anymore

            for (int i = 0; i < numberOfInputValues; i++)
            {
                PdfRange domain = GetDomainForInput(i);
                PdfRange encodeValues = GetEncodeForParameter(i)!.Value;
                input[i] = ClipToRange(input[i], domain.Min, domain.Max);
                input[i] = Interpolate(input[i], domain.Min, domain.Max, encodeValues.Min, encodeValues.Max);
                input[i] = ClipToRange(input[i], 0, Size[i] - 1);
                inputPrev[i] = (int)Math.Floor(input[i]);
                inputNext[i] = (int)Math.Ceiling(input[i]);
            }

            double[] outputValues = new RInterpol(inputPrev, inputNext, input.Length, numberOfOutputValues, Size, GetSamples())
                    .RInterpolate(input);

            for (int i = 0; i < numberOfOutputValues; i++)
            {
                PdfRange range = GetRangeForOutput(i);
                PdfRange? decodeValues = GetDecodeForParameter(i);
                if (!decodeValues.HasValue)
                {
                    throw new IOException("Range missing in function /Decode entry");
                }
                outputValues[i] = Interpolate(outputValues[i], 0, maxSample, decodeValues.Value.Min, decodeValues.Value.Max);
                outputValues[i] = ClipToRange(outputValues[i], range.Min, range.Max);
            }

            return outputValues;
        }
    }
}
