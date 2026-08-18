namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// A write-only <see cref="Stream"/> that accumulates its content in fixed-size pooled blocks
    /// and hands back a single exactly-sized array at the end.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Decoding filters cannot know the decompressed length up front, so they used to write into a
    /// <see cref="MemoryStream"/> seeded from the *compressed* length. That doubles its buffer as it
    /// fills, so a stream that inflates to 8 MB allocated roughly 16 MB of throwaway arrays on the
    /// way there — all of it above the 85 KB large-object threshold, and all of it repeated on every
    /// re-decode of the same page. It also returned the over-allocated buffer, keeping up to twice
    /// the needed memory alive.
    /// </para>
    /// <para>
    /// Writing into pooled blocks instead keeps the intermediate growth out of the GC heap entirely,
    /// and the single <see cref="ToArray"/> allocation is exactly the decoded length.
    /// <see cref="BlockSize"/> stays under the large-object threshold so the blocks are served from
    /// (and returned to) the shared array pool's small buckets.
    /// </para>
    /// </remarks>
    internal sealed class PooledBlockStream : Stream
    {
        private const int BlockSize = 64 * 1024;

        private readonly List<byte[]> blocks = new List<byte[]>();
        private int positionInBlock = BlockSize;
        private long length;
        private bool disposed;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => length;

        public override long Position
        {
            get => length;
            set => throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

#if NET
        public override void Write(ReadOnlySpan<byte> buffer)
#else
        public void Write(ReadOnlySpan<byte> buffer)
#endif
        {
            while (!buffer.IsEmpty)
            {
                if (positionInBlock == BlockSize)
                {
                    blocks.Add(ArrayPool<byte>.Shared.Rent(BlockSize));
                    positionInBlock = 0;
                }

                byte[] current = blocks[blocks.Count - 1];
                int toCopy = Math.Min(BlockSize - positionInBlock, buffer.Length);

                buffer.Slice(0, toCopy).CopyTo(new Span<byte>(current, positionInBlock, toCopy));

                positionInBlock += toCopy;
                length += toCopy;
                buffer = buffer.Slice(toCopy);
            }
        }

        public override void WriteByte(byte value)
        {
            if (positionInBlock == BlockSize)
            {
                blocks.Add(ArrayPool<byte>.Shared.Rent(BlockSize));
                positionInBlock = 0;
            }

            blocks[blocks.Count - 1][positionInBlock++] = value;
            length++;
        }

        /// <summary>
        /// Copies everything written so far into a new array of exactly <see cref="Length"/> bytes.
        /// </summary>
        public byte[] ToArray()
        {
            if (length > int.MaxValue)
            {
                throw new InvalidOperationException($"Cannot create an array of {length} bytes for the decoded stream.");
            }

            var result = new byte[length];
            int offset = 0;

            for (int i = 0; i < blocks.Count; ++i)
            {
                int count = i == blocks.Count - 1 ? positionInBlock : BlockSize;
                Array.Copy(blocks[i], 0, result, offset, count);
                offset += count;
            }

            return result;
        }

        public override void Flush()
        {
            // Nothing is buffered outside the blocks.
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;

                foreach (byte[] block in blocks)
                {
                    ArrayPool<byte>.Shared.Return(block);
                }

                blocks.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
