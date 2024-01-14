namespace UglyToad.PdfPig.Filters.PdfJs
{
    using System.Collections.Generic;

    public readonly struct PrecinctsSize
    {
        public int PPx { get; }
        public int PPy { get; }

        public PrecinctsSize(int ppx, int ppy)
        {
            PPx = ppx;
            PPy = ppy;
        }
    }

    public record Cod
    {
        public bool entropyCoderWithCustomPrecincts { get; set; }
        public bool sopMarkerUsed { get; set; }
        public bool ephMarkerUsed { get; set; }

        public byte progressionOrder { get; set; }
        public ushort layersCount { get; set; }
        public byte multipleComponentTransform { get; set; }

        public byte decompositionLevelsCount { get; set; }

        public int xcb { get; set; }
        public int ycb { get; set; }
        public bool selectiveArithmeticCodingBypass { get; set; }
        public bool resetContextProbabilities { get; set; }
        public bool terminationOnEachCodingPass { get; set; }
        public bool verticallyStripe { get; set; }
        public bool predictableTermination { get; set; }
        public bool segmentationSymbolUsed { get; set; }

        public byte reversibleTransformation { get; set; }

        public IReadOnlyList<PrecinctsSize> precinctsSizes { get; set; }
    }

    public struct Spqcd
    {
        public int epsilon { get; set; }
        public int mu { get; set; }
    }

    public struct Qcx
    {
        public bool noQuantization { get; set; }
        public bool scalarExpounded { get; set; }
        public int guardBits { get; set; }

        public IReadOnlyList<Spqcd> SPqcds { get; set; }
    }

    public record Tile
    {
        public ushort index { get; set; }
        public uint length { get; set; }
        public uint dataEnd { get; set; }

        public byte partIndex { get; set; }
        public byte partsCount { get; set; }

        public object COD { get; set; }
        public object COC { get; set; }
        public object QCD { get; set; }
        public Qcx[] QCC { get; set; }

        public int tx0 { get; set; }
        public int ty0 { get; set; }
        public int tx1 { get; set; }
        public int ty1 { get; set; }

        public int width { get; set; }
        public int height { get; set; }

        public List<Component> components { get; set; } // A guess
    }

    public record Context
    {
        public bool mainHeader { get; internal set; }

        public Siz SIZ { get; internal set; }

        public Qcx[] QCC { get; internal set; }

        public object[] COC { get; internal set; }

        public Qcx QCD { get; internal set; }

        public Cod COD { get; internal set; }

        public IReadOnlyList<Component> components { get; internal set; }

        public Tile currentTile { get; internal set; }

        public IReadOnlyList<Tile> tiles { get; internal set; }
    }

    public struct Component
    {
        public int precision { get; set; }
        public bool isSigned { get; set; }
        public byte XRsiz { get; set; }
        public byte YRsiz { get; set; }

        public int x0 { get; set; }
        public int x1 { get; set; }
        public int y0 { get; set; }
        public int y1 { get; set; }
        public int width { get; set; }
        public int height { get; set; }

        public int tcx0 { get; set; }
        public int tcy0 { get; set; }
        public int tcx1 { get; set; }
        public int tcy1 { get; set; }
    }

    public readonly struct Siz
    {
        public uint Xsiz { get; }
        public uint Ysiz { get; }
        public uint XOsiz { get; }
        public uint YOsiz { get; }
        public uint XTsiz { get; }
        public uint YTsiz { get; }
        public uint XTOsiz { get; }
        public uint YTOsiz { get; }
        public ushort Csiz { get; }

        public Siz(uint xsiz, uint ysiz, uint xOsiz, uint yOsiz, uint xTsiz, uint yTsiz, uint xTOsiz, uint yTOsiz, ushort csiz)
        {
            Xsiz = xsiz;
            Ysiz = ysiz;
            XOsiz = xOsiz;
            YOsiz = yOsiz;
            XTsiz = xTsiz;
            YTsiz = yTsiz;
            XTOsiz = xTOsiz;
            YTOsiz = yTOsiz;
            XTOsiz = xTOsiz;
            YTOsiz = yTOsiz;
            Csiz = csiz;
        }
    }
}
