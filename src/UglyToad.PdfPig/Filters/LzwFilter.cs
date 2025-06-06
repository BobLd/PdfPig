﻿#nullable disable

namespace UglyToad.PdfPig.Filters
{
    using Core;
    using System;
    using System.Collections.Generic;
    using Lzw;
    using System.IO;
    using Tokens;
    using Util;

    /// <summary>
    /// The LZW (Lempel-Ziv-Welch) filter is a variable-length, adaptive compression method
    /// that has been adopted as one of the standard compression methods in the Tag Image File Format (TIFF) standard. 
    /// </summary>
    public sealed class LzwFilter : IFilter
    {
        private const int DefaultColors = 1;
        private const int DefaultBitsPerComponent = 8;
        private const int DefaultColumns = 1;

        private const int ClearTable = 256;
        private const int EodMarker = 257;

        private const int NineBitBoundary = 511;
        private const int TenBitBoundary = 1023;
        private const int ElevenBitBoundary = 2047;
        
        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            var parameters = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);

            var predictor = parameters.GetIntOrDefault(NameToken.Predictor, -1);

            var earlyChange = parameters.GetIntOrDefault(NameToken.EarlyChange, 1);

            var colors = Math.Min(parameters.GetIntOrDefault(NameToken.Colors, DefaultColors), 32);
            var bitsPerComponent = parameters.GetIntOrDefault(NameToken.BitsPerComponent, DefaultBitsPerComponent);
            var columns = parameters.GetIntOrDefault(NameToken.Columns, DefaultColumns);

            return Decode(input.Span, earlyChange == 1, predictor, colors, bitsPerComponent, columns);
        }

        private static Memory<byte> Decode(ReadOnlySpan<byte> input, bool isEarlyChange, int predictor, int colors, int bitsPerComponent, int columns)
        {
            using (var output = new MemoryStream((int)(input.Length * 1.5))) // A guess.
            using (var result = PngPredictor.WrapPredictor(output, predictor, colors, bitsPerComponent, columns))
            {
                var table = GetDefaultTable();

                var codeBits = 9;

                var data = new BitStream(input);

                var codeOffset = isEarlyChange ? 0 : 1;

                var previous = -1;

                while (true)
                {
                    var next = data.Get(codeBits);

                    if (next == EodMarker)
                    {
                        break;
                    }

                    if (next == ClearTable)
                    {
                        table = GetDefaultTable();
                        previous = -1;
                        codeBits = 9;
                        continue;
                    }

                    if (table.TryGetValue(next, out var b))
                    {
                        result.Write(b,0, b.Length);

                        if (previous >= 0)
                        {
                            var lastSequence = table[previous];

                            var newSequence = new byte[lastSequence.Length + 1];

                            Array.Copy(lastSequence, newSequence, lastSequence.Length);

                            newSequence[lastSequence.Length] = b[0];

                            table[table.Count] = newSequence;
                        }
                    }
                    else
                    {
                        var lastSequence = table[previous];

                        var newSequence = new byte[lastSequence.Length + 1];

                        Array.Copy(lastSequence, newSequence, lastSequence.Length);

                        newSequence[lastSequence.Length] = lastSequence[0];

                        result.Write(newSequence, 0, newSequence.Length);
                        
                        table[table.Count] = newSequence;
                    }

                    previous = next;

                    if (table.Count >= ElevenBitBoundary + codeOffset)
                    {
                        codeBits = 12;
                    }
                    else if (table.Count >= TenBitBoundary + codeOffset)
                    {
                        codeBits = 11;
                    }
                    else if (table.Count >= NineBitBoundary + codeOffset)
                    {
                        codeBits = 10;
                    }
                    else
                    {
                        codeBits = 9;
                    }
                }

                result.Flush();

                return output.AsMemory();
            }
        }

        private static Dictionary<int, byte[]> GetDefaultTable()
        {
            var table = new Dictionary<int, byte[]>();

            for (var i = 0; i < 256; i++)
            {
                table[i] = [(byte)i];
            }

            table[ClearTable] = null;
            table[EodMarker] = null;

            return table;
        }
    }
}