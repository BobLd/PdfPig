namespace UglyToad.PdfPig.PdfFonts.Cmap
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Parser;
    using System.Text;

    internal static class CMapCache
    {
        private static readonly Dictionary<string, CMap> Cache = new Dictionary<string, CMap>(StringComparer.OrdinalIgnoreCase);
        private static readonly object Lock = new object();

        private static readonly CMapParser CMapParser = new CMapParser();

        public static bool TryGet(string name, [NotNullWhen(true)] out CMap? result)
        {
            result = null;

            lock (Lock)
            {
                if (Cache.TryGetValue(name, out result))
                {
                    return true;
                }

                if (CMapParser.TryParseExternal(name, out result))
                {

                    Cache[name] = result;

                    return true;
                }

                return false;
            }
        }

        public static CMap Parse(IInputBytes bytes)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var result = CMapParser.Parse(bytes);

            return result;
        }

        private static ReadOnlySpan<byte> cmapNameTag => @"/CMapName "u8;

        public static bool TryGetNameFast(ReadOnlySpan<byte> bytes, out string? name)
        {
            name = null;
            int nameIndex = bytes.IndexOf(cmapNameTag);
            
            if (nameIndex <= -1)
            {
                return false;
            }

            int nameEndIndex = bytes.Slice(nameIndex + cmapNameTag.Length).IndexOf("def"u8);

            if (nameEndIndex <= -1)
            {
                return false;
            }

            name = Encoding.UTF8.GetString(bytes.Slice(nameIndex + cmapNameTag.Length, nameEndIndex - 1));
            return true;
        }
    }
}
