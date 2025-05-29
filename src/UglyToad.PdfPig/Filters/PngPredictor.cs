namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.IO;

    internal static class PngPredictor
    {
        public static void DecodePredictorRow(int predictor, int colors, int bitsPerComponent, int columns, byte[] actline, byte[] lastline)
        {
            if (predictor == 1)
            {
                // no prediction
                return;
            }

            int bitsPerPixel = colors * bitsPerComponent;
            int bytesPerPixel = (bitsPerPixel + 7) / 8;
            int rowLength = actline.Length;

            switch (predictor)
            {
                case 2:
                    // PRED TIFF SUB
                    if (bitsPerComponent == 8)
                    {
                        for (int p = bytesPerPixel; p < rowLength; p++)
                        {
                            int sub = actline[p] & 0xff;
                            int left = actline[p - bytesPerPixel] & 0xff;
                            actline[p] = (byte)(sub + left);
                        }
                    }
                    else if (bitsPerComponent == 16)
                    {
                        for (int p = bytesPerPixel; p < rowLength - 1; p += 2)
                        {
                            int sub = ((actline[p] & 0xff) << 8) + (actline[p + 1] & 0xff);
                            int left = ((actline[p - bytesPerPixel] & 0xff) << 8) + (actline[p - bytesPerPixel + 1] & 0xff);
                            int sum = sub + left;
                            actline[p] = (byte)((sum >> 8) & 0xff);
                            actline[p + 1] = (byte)(sum & 0xff);
                        }
                    }
                    else if (bitsPerComponent == 1 && colors == 1)
                    {
                        for (int p = 0; p < rowLength; p++)
                        {
                            for (int bit = 7; bit >= 0; --bit)
                            {
                                int sub = (actline[p] >> bit) & 1;
                                if (p == 0 && bit == 7)
                                {
                                    continue;
                                }

                                int left;
                                if (bit == 7)
                                {
                                    left = actline[p - 1] & 1;
                                }
                                else
                                {
                                    left = (actline[p] >> (bit + 1)) & 1;
                                }

                                if (((sub + left) & 1) == 0)
                                {
                                    actline[p] &= (byte)~(1 << bit);
                                }
                                else
                                {
                                    actline[p] |= (byte)(1 << bit);
                                }
                            }
                        }
                    }
                    else
                    {
                        int elements = columns * colors;
                        for (int p = colors; p < elements; ++p)
                        {
                            int bytePosSub = p * bitsPerComponent / 8;
                            int bitPosSub = 8 - p * bitsPerComponent % 8 - bitsPerComponent;
                            int bytePosLeft = (p - colors) * bitsPerComponent / 8;
                            int bitPosLeft = 8 - (p - colors) * bitsPerComponent % 8 - bitsPerComponent;
                            int sub = GetBitSeq(actline[bytePosSub], bitPosSub, bitsPerComponent);
                            int left = GetBitSeq(actline[bytePosLeft], bitPosLeft, bitsPerComponent);
                            actline[bytePosSub] = (byte)CalcSetBitSeq(actline[bytePosSub], bitPosSub, bitsPerComponent, sub + left);
                        }
                    }
                    break;

                case 10:
                    // PRED NONE
                    // do nothing
                    break;

                case 11:
                    // PRED SUB
                    for (int p = bytesPerPixel; p < rowLength; p++)
                    {
                        int sub = actline[p];
                        int left = actline[p - bytesPerPixel];
                        actline[p] = (byte)(sub + left);
                    }
                    break;

                case 12:
                    // PRED UP
                    for (int p = 0; p < rowLength; p++)
                    {
                        int up = actline[p] & 0xff;
                        int prior = lastline[p] & 0xff;
                        actline[p] = (byte)((up + prior) & 0xff);
                    }
                    break;

                case 13:
                    // PRED AVG
                    for (int p = 0; p < rowLength; p++)
                    {
                        int avg = actline[p] & 0xff;
                        int left = p - bytesPerPixel >= 0 ? actline[p - bytesPerPixel] & 0xff : 0;
                        int up = lastline[p] & 0xff;
                        actline[p] = (byte)((avg + (left + up) / 2) & 0xff);
                    }
                    break;

                case 14:
                    // PRED PAETH
                    for (int p = 0; p < rowLength; p++)
                    {
                        int paeth = actline[p] & 0xff;
                        int a = p - bytesPerPixel >= 0 ? actline[p - bytesPerPixel] & 0xff : 0;
                        int b = lastline[p] & 0xff;
                        int c = p - bytesPerPixel >= 0 ? lastline[p - bytesPerPixel] & 0xff : 0;
                        int value = a + b - c;
                        int absa = Math.Abs(value - a);
                        int absb = Math.Abs(value - b);
                        int absc = Math.Abs(value - c);
                        
                        if (absa <= absb && absa <= absc)
                        {
                            actline[p] = (byte)((paeth + a) & 0xff);
                        }
                        else if (absb <= absc)
                        {
                            actline[p] = (byte)((paeth + b) & 0xff);
                        }
                        else
                        {
                            actline[p] = (byte)((paeth + c) & 0xff);
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        public static int CalculateRowLength(int colors, int bitsPerComponent, int columns)
        {
            int bitsPerPixel = colors * bitsPerComponent;
            return (columns * bitsPerPixel + 7) / 8;
        }

        private static int GetBitSeq(int by, int startBit, int bitSize)
        {
            int mask = ((1 << bitSize) - 1);
            return (by >> startBit) & mask;
        }

        private static int CalcSetBitSeq(int by, int startBit, int bitSize, int val)
        {
            int mask = ((1 << bitSize) - 1);
            int truncatedVal = val & mask;
            mask = ~(mask << startBit);
            return (by & mask) | (truncatedVal << startBit);
        }

        public static Stream WrapPredictor(Stream outStream, int predictor, int colors, int bitsPerComponent, int columns)
        {
            if (predictor > 1)
            {
                return new PredictorOutputStream(outStream, predictor, colors, bitsPerComponent, columns);
            }

            return outStream;
        }

        private sealed class PredictorOutputStream : Stream
        {
            private readonly Stream _baseStream;
            private int _predictor;
            private readonly int _colors;
            private readonly int _bitsPerComponent;
            private readonly int _columns;
            private readonly int _rowLength;
            private readonly bool _predictorPerRow;
            private byte[] _currentRow;
            private byte[] _lastRow;
            private int _currentRowData = 0;
            private bool _predictorRead = false;

            public PredictorOutputStream(Stream baseStream, int predictor, int colors, int bitsPerComponent, int columns)
            {
                _baseStream = baseStream;
                _predictor = predictor;
                _colors = colors;
                _bitsPerComponent = bitsPerComponent;
                _columns = columns;
                _rowLength = CalculateRowLength(colors, bitsPerComponent, columns);
                _predictorPerRow = predictor >= 10;
                _currentRow = new byte[_rowLength];
                _lastRow = new byte[_rowLength];
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                int currentOffset = offset;
                int maxOffset = currentOffset + count;
                while (currentOffset < maxOffset)
                {
                    if (_predictorPerRow && _currentRowData == 0 && !_predictorRead)
                    {
                        // PNG predictor; each row starts with predictor type (0, 1, 2, 3, 4)
                        _predictor = buffer[currentOffset] + 10;
                        currentOffset++;
                        _predictorRead = true;
                    }
                    else
                    {
                        int toRead = Math.Min(_rowLength - _currentRowData, maxOffset - currentOffset);
                        Array.Copy(buffer, currentOffset, _currentRow, _currentRowData, toRead);
                        _currentRowData += toRead;
                        currentOffset += toRead;

                        if (_currentRowData == _currentRow.Length)
                        {
                            DecodeAndWriteRow();
                        }
                    }
                }
            }

            private void DecodeAndWriteRow()
            {
                DecodePredictorRow(_predictor, _colors, _bitsPerComponent, _columns, _currentRow, _lastRow);
                _baseStream.Write(_currentRow, 0, _currentRow.Length);
                FlipRows();
            }

            private void FlipRows()
            {
                (_lastRow, _currentRow) = (_currentRow, _lastRow);
                _currentRowData = 0;
                _predictorRead = false;
            }

            public override void Flush()
            {
                // The last row is allowed to be incomplete, and should be completed with zeros.
                if (_currentRowData > 0)
                {
                    // public static void fill(int[] a, int fromIndex, int toIndex, int val)
                    // Arrays.fill(currentRow, currentRowData, rowLength, (byte)0);
                    _currentRow.AsSpan(_currentRowData, _rowLength - _currentRowData).Fill(byte.MinValue);

                    DecodeAndWriteRow();
                }
                _baseStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException("Read not supported");
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void WriteByte(byte value) => throw new NotSupportedException("Not supported");
        }
    }
}
