using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace JpegLibrary
{
    public readonly struct JpegAdobeApplicationSpecific
    {
        private static readonly byte[] _adobeKey = Encoding.ASCII.GetBytes("Adobe");

        public byte[] DctVersion { get; }

        public byte[] Flags0 { get; }

        public byte[] Flags1 { get; }

        public byte ColorTransformCode { get; }

        private JpegAdobeApplicationSpecific(byte[] dctVersion, byte[] flags0, byte[] flags1, byte colorTransformCode)
        {
            DctVersion = dctVersion;
            Flags0 = flags0;
            Flags1 = flags1;
            ColorTransformCode = colorTransformCode;
        }

        public static bool TryParse(ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out JpegAdobeApplicationSpecific? adobeApplicationSpecific)
        {
#if NO_READONLYSEQUENCE_FISTSPAN
            ReadOnlySpan<byte> firstSpan = buffer.First.Span;
#else
            ReadOnlySpan<byte> firstSpan = buffer.FirstSpan;
#endif

            // See 'Adobe Technical Note #5116'

            // DCTDecode ignores and skips any APPE marker segment that does not begin with the ‘Adobe’ 5-character string.
            if (firstSpan.Length >= 12 && MemoryExtensions.SequenceEqual(_adobeKey, firstSpan.Slice(0, 5)))
            {
                // Two-byte DCTEncode/DCTDecode version number (presently X’65)
                byte[] dctVersion = firstSpan.Slice(5, 2).ToArray();

                // Two-byte flags0 0x8000 bit: Encoder used Blend=1 downsampling
                byte[] flags0 = firstSpan.Slice(7, 2).ToArray();

                // Two-byte flags1
                byte[] flags1 = firstSpan.Slice(9, 2).ToArray();

                // One-byte color transform code
                byte colorTransformCode = firstSpan.Slice(11, 1)[0];

                adobeApplicationSpecific = new JpegAdobeApplicationSpecific(dctVersion, flags0, flags1, colorTransformCode);
                return true;
            }

            adobeApplicationSpecific = null;
            return false;
        }
    }
}
