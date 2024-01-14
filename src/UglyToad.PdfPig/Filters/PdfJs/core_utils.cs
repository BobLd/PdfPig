namespace UglyToad.PdfPig.Filters.PdfJs
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.Text;

    public static class core_utils
    {

        public static ushort readUint16(this ReadOnlySpan<byte> data, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset));
            }

            return BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
            //return (data[offset] << 8) | data[offset + 1];
        }

        public static uint readUint32(this ReadOnlySpan<byte> data, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));
            }

            return BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
            //return (
            //    ((data[offset] << 24) |
            //     (data[offset + 1] << 16) |
            //     (data[offset + 2] << 8) |
            //     data[offset + 3]) >>>
            //    0
            //);
        }
    }
}
