﻿namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System;
    using System.IO;

    internal sealed class ImageInputStream : AbstractImageInputStream
    {
        private readonly Stream inner;

        /// <inheritdoc />
        public override long Length => inner.Length;

        /// <inheritdoc />
        public override long Position => inner.Position;

        /// <summary>
        /// Constructs a <see cref="ImageInputStream"/> that will read the image data
        /// from a given byte array.
        /// </summary>
        /// <param name="bytes"></param>
        public ImageInputStream(byte[] bytes)
            : this(new MemoryStream(bytes ??
                throw new ArgumentNullException(nameof(bytes))))
        {
        }

        /// <summary>
        /// Constructs a <see cref="ImageInputStream"/> that will read the image data
        /// from a given <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">the <see cref="Stream"/> to read the image data from.></param>
        public ImageInputStream(Stream input)
        {
            inner = input ?? throw new ArgumentNullException(nameof(input));
        }

        /// <inheritdoc />
        public override void Seek(long pos)
        {
            SetBitOffset(0);
            inner.Position = pos;
        }

        /// <inheritdoc />
        public override int Read()
        {
            if (IsAtEnd())
            {
                return -1;
            }

            SetBitOffset(0);
            return inner.ReadByte();
        }

        /// <inheritdoc />
        public override int Read(Span<byte> b, int off, int len)
        {
            if (IsAtEnd())
            {
                throw new EndOfStreamException();
            }

            SetBitOffset(0);

            return inner.Read(b.Slice(0, len));
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            inner.Dispose();
        }
    }
}