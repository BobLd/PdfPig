namespace UglyToad.PdfPig.Images
{
    using System;
    using System.Buffers.Binary;

    internal static class Jpeg2000Helper
    {
        public static byte GetJp2BitsPerComponent(ReadOnlySpan<byte> jp2Bytes)
        {
            // Ensure the input has at least 12 bytes for the signature box
            if (jp2Bytes.Length < 12)
            {
                throw new InvalidOperationException("Input is too short to be a valid JP2 file.");
            }
            
            // Verify the JP2 signature box
            uint length = ReadBigEndianUInt32(jp2Bytes.Slice(0, 4));
            uint type = ReadBigEndianUInt32(jp2Bytes.Slice(4, 4));
            uint magic = ReadBigEndianUInt32(jp2Bytes.Slice(8, 4));
            
            if (length != 0x0000000C || type != 0x6A502020 || magic != 0x0D0A870A)
            {
                throw new InvalidOperationException("Invalid JP2 signature box.");
            }
            
            // Proceed to parse JP2 boxes
            return ParseBoxes(jp2Bytes.Slice(12));
        }
        
        private static byte ParseBoxes(ReadOnlySpan<byte> jp2Bytes)
        {
            int offset = 0;
            while (offset < jp2Bytes.Length)
            {
                if (offset + 8 > jp2Bytes.Length)
                {
                    throw new InvalidOperationException("Invalid JP2 box structure.");
                }
                
                // Read box length and type
                uint boxLength = ReadBigEndianUInt32(jp2Bytes.Slice(offset, 4));
                uint boxType = ReadBigEndianUInt32(jp2Bytes.Slice(offset + 4, 4));
                
                // Check for the contiguous codestream box ('jp2c')
                if (boxType == 0x6A703263) // 'jp2c' in ASCII
                {
                    // Parse the codestream to find the SIZ marker
                    return ParseCodestream(jp2Bytes.Slice(offset + 8));
                }
                
                // Move to the next box
                offset += (int)(boxLength > 0 ? boxLength : 8); // Box length of 0 means the rest of the file
            }
            
            throw new InvalidOperationException("Codestream box not found in JP2 file.");
        }
        
        private static byte ParseCodestream(ReadOnlySpan<byte> codestream)
        {
            int offset = 0;
            while (offset + 2 <= codestream.Length)
            {
                // Read marker (2 bytes)
                ushort marker = ReadBigEndianUInt16(codestream.Slice(offset, 2));
                
                // Check for SIZ marker (0xFF51)
                if (marker == 0xFF51)
                {
                    if (offset + 38 > codestream.Length)
                    {
                        throw new InvalidOperationException("Invalid SIZ marker structure.");
                    }
                    
                    // Skip marker length (2 bytes), capabilities (4 bytes), and reference grid size (8 bytes)
                    // Skip image offset (8 bytes), tile size (8 bytes), and tile offset (8 bytes)
                    offset += 38;
                    
                    // Read number of components (2 bytes)
                    ushort numComponents = ReadBigEndianUInt16(codestream.Slice(offset, 2));
                    
                    offset += 2;
                    if (numComponents < 1)
                    {
                        throw new InvalidOperationException("Invalid number of components in SIZ marker.");
                    }
                    
                    // Read bits per component for the first component (1 byte per component)
                    byte bitsPerComponent = codestream[offset];
                    
                    // Bits per component is stored as (bits - 1)
                    return ++bitsPerComponent; // + (byte)1;
                }
                // Move to the next marker
                offset += 2;
            }
            
            throw new InvalidOperationException("SIZ marker not found in JPEG2000 codestream.");
        }
        
        private static uint ReadBigEndianUInt32(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 4)
            {
                throw new ArgumentException("Not enough bytes to read a 32-bit integer.");
            }

            return BinaryPrimitives.ReadUInt32BigEndian(bytes);

            //return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
        }
        
        private static ushort ReadBigEndianUInt16(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 2)
            {
                throw new ArgumentException("Not enough bytes to read a 16-bit integer.");
            }

            return BinaryPrimitives.ReadUInt16BigEndian(bytes);

            //return (ushort)(bytes[0] << 8 | bytes[1]);
        }
    }
}
