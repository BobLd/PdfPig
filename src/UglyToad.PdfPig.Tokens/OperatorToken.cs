namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Collections.Generic;

    /// <inheritdoc />
    /// <summary>
    /// An operator token encountered in a page content or Adobe Type 1 font stream.
    /// </summary>
    public class OperatorToken : IDataToken<string>
    {
        private static readonly object Lock = new object();
        private static readonly Dictionary<string, string> PooledNames = new Dictionary<string, string>();

        /// <summary>
        /// Begin text.
        /// </summary>
        public static readonly OperatorToken Bt = new OperatorToken("BT");

        /// <summary>
        /// Def.
        /// </summary>
        public static readonly OperatorToken Def = new OperatorToken("def");

        /// <summary>
        /// Dict.
        /// </summary>
        public static readonly OperatorToken Dict = new OperatorToken("dict");

        /// <summary>
        /// Dup.
        /// </summary>
        public static readonly OperatorToken Dup = new OperatorToken("dup");

        /// <summary>
        /// Eexec.
        /// </summary>
        public static readonly OperatorToken Eexec = new OperatorToken("eexec");

        /// <summary>
        /// End object.
        /// </summary>
        public static readonly OperatorToken EndObject = new OperatorToken("endobj");

        /// <summary>
        /// End stream.
        /// </summary>
        public static readonly OperatorToken EndStream = new OperatorToken("endstream");

        /// <summary>
        /// End text.
        /// </summary>
        public static readonly OperatorToken Et = new OperatorToken("ET");

        /// <summary>
        /// For.
        /// </summary>
        public static readonly OperatorToken For = new OperatorToken("for");

        /// <summary>
        /// N.
        /// </summary>
        public static readonly OperatorToken N = new OperatorToken("n");

        /// <summary>
        /// Put.
        /// </summary>
        public static readonly OperatorToken Put = new OperatorToken("put");

        /// <summary>
        /// Pop.
        /// </summary>
        public static readonly OperatorToken QPop = new OperatorToken("Q");

        /// <summary>
        /// Push.
        /// </summary>
        public static readonly OperatorToken QPush = new OperatorToken("q");

        /// <summary>
        /// R.
        /// </summary>
        public static readonly OperatorToken R = new OperatorToken("R");

        /// <summary>
        /// Rectangle.
        /// </summary>
        public static readonly OperatorToken Re = new OperatorToken("re");

        /// <summary>
        /// Readonly.
        /// </summary>
        public static readonly OperatorToken Readonly = new OperatorToken("readonly");

        /// <summary>
        /// Object.
        /// </summary>
        public static readonly OperatorToken StartObject = new OperatorToken("obj");

        /// <summary>
        /// Stream.
        /// </summary>
        public static readonly OperatorToken StartStream = new OperatorToken("stream");

        /// <summary>
        /// Set font and size.
        /// </summary>
        public static readonly OperatorToken Tf = new OperatorToken("Tf");

        /// <summary>
        /// Modify clipping.
        /// </summary>
        public static readonly OperatorToken WStar = new OperatorToken("W*");

        /// <summary>
        /// Cross reference.
        /// </summary>
        public static readonly OperatorToken Xref = new OperatorToken("xref");

        /// <summary>
        /// Cross reference section offset.
        /// </summary>
        public static readonly OperatorToken StartXref = new OperatorToken("startxref");

        /// <summary>
        /// Shared instances for every operator that can appear in a content stream (PDF 1.7,
        /// Table A.1) plus the short file-structure and Type 1 keywords, keyed by the operator
        /// name packed into an <see cref="int"/>.
        /// </summary>
        /// <remarks>
        /// A page's content stream is re-parsed on every render and a path- or text-heavy page
        /// emits hundreds of thousands of operators. Without this pool each occurrence allocated
        /// a string (from <c>data.ToString()</c>) plus an <see cref="OperatorToken"/>, and
        /// contended <see cref="Lock"/> to intern the name. Operator names are 1-3 ASCII
        /// characters, which is what makes the packed key possible; longer or unrecognised names
        /// still take the allocating path. The dictionary is only written by
        /// <see cref="BuildPool"/> during static initialisation, so concurrent reads need no lock.
        /// </remarks>
        private static readonly Dictionary<int, OperatorToken> Pooled = BuildPool();

        private static Dictionary<int, OperatorToken> BuildPool()
        {
            OperatorToken[] tokens =
            [
                // Instances already exposed as static fields, so that reference comparisons
                // against them keep holding for pooled lookups.
                Bt, Def, Dict, Dup, Et, For, N, Put, QPop, QPush, R, Re, StartObject, Tf, WStar,

                // General/special graphics state
                new OperatorToken("w"), new OperatorToken("J"), new OperatorToken("j"),
                new OperatorToken("M"), new OperatorToken("d"), new OperatorToken("ri"),
                new OperatorToken("i"), new OperatorToken("gs"), new OperatorToken("cm"),

                // Path construction and painting
                new OperatorToken("m"), new OperatorToken("l"), new OperatorToken("c"),
                new OperatorToken("v"), new OperatorToken("y"), new OperatorToken("h"),
                new OperatorToken("S"), new OperatorToken("s"), new OperatorToken("f"),
                new OperatorToken("F"), new OperatorToken("f*"), new OperatorToken("B"),
                new OperatorToken("B*"), new OperatorToken("b"), new OperatorToken("b*"),
                new OperatorToken("W"),

                // Text state, positioning and showing
                new OperatorToken("Tc"), new OperatorToken("Tw"), new OperatorToken("Tz"),
                new OperatorToken("TL"), new OperatorToken("Tr"), new OperatorToken("Ts"),
                new OperatorToken("Td"), new OperatorToken("TD"), new OperatorToken("Tm"),
                new OperatorToken("T*"), new OperatorToken("Tj"), new OperatorToken("TJ"),
                new OperatorToken("'"), new OperatorToken("\""),

                // Type 3 font glyph metrics
                new OperatorToken("d0"), new OperatorToken("d1"),

                // Colour
                new OperatorToken("CS"), new OperatorToken("cs"), new OperatorToken("SC"),
                new OperatorToken("sc"), new OperatorToken("SCN"), new OperatorToken("scn"),
                new OperatorToken("G"), new OperatorToken("g"), new OperatorToken("RG"),
                new OperatorToken("rg"), new OperatorToken("K"), new OperatorToken("k"),

                // Shadings, XObjects and inline images
                new OperatorToken("sh"), new OperatorToken("Do"), new OperatorToken("BI"),
                new OperatorToken("ID"), new OperatorToken("EI"),

                // Marked content and compatibility
                new OperatorToken("MP"), new OperatorToken("DP"), new OperatorToken("BMC"),
                new OperatorToken("BDC"), new OperatorToken("EMC"), new OperatorToken("BX"),
                new OperatorToken("EX")
            ];

            var pool = new Dictionary<int, OperatorToken>(tokens.Length);

            foreach (OperatorToken token in tokens)
            {
                if (TryGetPackedKey(token.Data.AsSpan(), out int key))
                {
                    pool[key] = token;
                }
            }

            return pool;
        }

        /// <summary>
        /// Packs a 1-3 character ASCII operator name into an int. The length is kept in the low
        /// byte so that names of different lengths can never collide.
        /// </summary>
        private static bool TryGetPackedKey(ReadOnlySpan<char> data, out int key)
        {
            key = 0;

            if (data.Length is < 1 or > 3)
            {
                return false;
            }

            int packed = data.Length;

            for (int i = 0; i < data.Length; ++i)
            {
                char c = data[i];

                if (c > 127)
                {
                    return false;
                }

                packed |= c << (8 * (i + 1));
            }

            key = packed;
            return true;
        }

        /// <inheritdoc />
        public string Data { get; }

        private OperatorToken(string data)
        {
            string stored;

            lock (Lock)
            {
                if (!PooledNames.TryGetValue(data, out stored))
                {
                    stored = data;
                    PooledNames[data] = stored;
                }
            }

            Data = stored;
        }

        /// <summary>
        /// Create a new <see cref="OperatorToken"/>.
        /// </summary>
        public static OperatorToken Create(ReadOnlySpan<char> data)
        {
            if (TryGetPackedKey(data, out int key) && Pooled.TryGetValue(key, out OperatorToken? pooled))
            {
                return pooled;
            }

            return data switch {
                "BT" => Bt,
                "eexec" => Eexec,
                "endobj" => EndObject,
                "endstream" => EndStream,
                "ET" => Et,
                "def" => Def,
                "dict" => Dict,
                "for" => For,
                "dup" => Dup,
                "n" => N,
                "obj" => StartObject,
                "put" => Put,
                "Q" => QPop,
                "q" => QPush,
                "R" => R,
                "re" => Re,
                "readonly" => Readonly,
                "stream" => StartStream,
                "Tf" => Tf,
                "W*" => WStar,
                "xref" => Xref,
                "startxref" => StartXref,
                _ => new OperatorToken(data.ToString())
            };
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is IToken token && Equals(token);
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not OperatorToken other)
            {
                return false;
            }

            return Data == other.Data;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Data;
        }
    }
}
