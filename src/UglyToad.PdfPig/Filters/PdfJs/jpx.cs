namespace UglyToad.PdfPig.Filters.PdfJs
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Runtime.Remoting.Contexts;

    public class jpx
    {
        public class JpxError : Exception
        {
            public JpxError(string message) : base($"JPX error: {message}")
            {
            }
        }

        // Table E.1
        Dictionary<string, int> SubbandsGainLog2 = new Dictionary<string, int>()
        {
            { "LL", 0 },
            { "LH", 1 },
            { "HL", 1 },
            { "HH", 2 }
        };

        public class JpxImage
        {
            public bool failOnCorruptedImage;

            public JpxImage()
            {
                this.failOnCorruptedImage = false;
            }

            public void parse(Span<byte> data)
            {
                ushort head = core_utils.readUint16(data, 0);

                // No box header, immediate start of codestream (SOC)
                if (head == 0xff4f)
                {
                    this.parseCodestream(data, 0, data.Length);
                    return;
                }

                int length = data.Length;
                int position = 0;

                while (position < length)
                {
                    int headerSize = 8;
                    long lbox = core_utils.readUint32(data, position);
                    uint tbox = core_utils.readUint32(data, position + 4);
                    position += headerSize;
                    if (lbox == 1)
                    {
                        // XLBox: read UInt64 according to spec.
                        // JavaScript's int precision of 53 bit should be sufficient here.
                        lbox =
                            core_utils.readUint32(data, position) * 4294967296 +
                            core_utils.readUint32(data, position + 4);
                        position += 8;
                        headerSize += 8;
                    }

                    if (lbox == 0)
                    {
                        lbox = length - position + headerSize;
                    }

                    if (lbox < headerSize)
                    {
                        throw new JpxError("Invalid box field size");
                    }

                    int dataLength = Convert.ToInt32(lbox - headerSize); // TODO - WARNING
                    bool jumpDataLength = true;
                    switch (tbox)
                    {
                        case 0x6a703268: // 'jp2h'
                            jumpDataLength = false; // parsing child boxes
                            break;
                        case 0x636f6c72: // 'colr'
                            // Colorspaces are not used, the CS from the PDF is used.
                            var method = data[position];
                            if (method == 1)
                            {
                                // enumerated colorspace
                                var colorspace = core_utils.readUint32(data, position + 3);
                                switch (colorspace)
                                {
                                    case 16: // this indicates a sRGB colorspace
                                    case 17: // this indicates a grayscale colorspace
                                    case 18: // this indicates a YUV colorspace
                                        break;
                                    default:
                                        System.Diagnostics.Debug.WriteLine("warn: Unknown colorspace " + colorspace);
                                        break;
                                }
                            }
                            else if (method == 2)
                            {
                                System.Diagnostics.Debug.WriteLine("info: ICC profile not supported");
                            }

                            break;
                        case 0x6a703263: // 'jp2c'
                            this.parseCodestream(data, position, position + dataLength);
                            break;
                        case 0x6a502020: // 'jP\024\024'
                            if (core_utils.readUint32(data, position) != 0x0d0a870a)
                            {
                                System.Diagnostics.Debug.WriteLine("warn: Invalid JP2 signature");
                            }

                            break;
                        // The following header types are valid but currently not used:
                        case 0x6a501a1a: // 'jP\032\032'
                        case 0x66747970: // 'ftyp'
                        case 0x72726571: // 'rreq'
                        case 0x72657320: // 'res '
                        case 0x69686472: // 'ihdr'
                            break;
                        default:
                            string headerType = new string(new char[]
                            {
                                (char)((tbox >> 24) & 0xff),
                                (char)((tbox >> 16) & 0xff),
                                (char)((tbox >> 8) & 0xff),
                                (char)(tbox & 0xff)
                            });
                            //var headerType = String.fromCharCode(
                            //  (tbox >> 24) & 0xff,
                            //  (tbox >> 16) & 0xff,
                            //  (tbox >> 8) & 0xff,
                            //  tbox & 0xff
                            //);
                            System.Diagnostics.Debug.WriteLine($"warn: Unsupported header type {tbox} ({headerType}).");
                            break;
                    }

                    if (jumpDataLength)
                    {
                        position += dataLength;
                    }
                }
            }


            void parseImageProperties(Stream stream)
            {

            }


            void parseCodestream(Span<byte> data, int start, int end)
            {
                var context = new Context();
                bool doNotRecover = false;

                try
                {
                    int position = start;
                    while (position + 1 < end)
                    {
                        var code = core_utils.readUint16(data, position);
                        position += 2;

                        int length = 0;
                        bool scalarExpounded;
                        int j, sqcd, spqcdSize;
                        Tile tile;
                        List<Spqcd> spqcds;

                        switch (code)
                        {
                            case 0xff4f: // Start of codestream (SOC)
                                context.mainHeader = true;
                                break;
                            case 0xffd9: // End of codestream (EOC)
                                break;
                            case 0xff51: // Image and tile size (SIZ)
                                length = core_utils.readUint16(data, position);
                                
                                var xsiz = core_utils.readUint32(data, position + 4);
                                var ysiz = core_utils.readUint32(data, position + 8);
                                var xOsiz = core_utils.readUint32(data, position + 12);
                                var yOsiz = core_utils.readUint32(data, position + 16);
                                var xTsiz = core_utils.readUint32(data, position + 20);
                                var yTsiz = core_utils.readUint32(data, position + 24);
                                var xTOsiz = core_utils.readUint32(data, position + 28);
                                var yTOsiz = core_utils.readUint32(data, position + 32);
                                var componentsCount = core_utils.readUint16(data, position + 36);
                                var csiz = componentsCount;

                                Siz siz = new Siz(xsiz, ysiz, xOsiz, yOsiz, xTsiz, yTsiz, xTOsiz, yTOsiz, csiz);

                                var components = new List<Component>();
                                j = position + 38;
                                for (int i = 0; i < componentsCount; i++)
                                {
                                    var component = new Component()
                                    {
                                        precision = (data[j] & 0x7f) + 1,
                                        isSigned = (data[j] & 0x80) != 0,
                                        XRsiz = data[j + 1],
                                        YRsiz = data[j + 2]
                                    };
                                    j += 3;
                                    calculateComponentDimensions(component, siz);
                                    components.Add(component);
                                }
                        context.SIZ = siz;
                        context.components = components;
                        calculateTileGrids(context, components);
                        context.QCC = [];
                        context.COC = [];
                        break;
          case 0xff5c: // Quantization default (QCD)
                            length = core_utils.readUint16(data, position);
                            Qcx qcd = new Qcx();
                            j = position + 2;
                            sqcd = data[j++];
                            switch (sqcd & 0x1f)
                            {
                                case 0:
                                    spqcdSize = 8;
                                    scalarExpounded = true;
                                    break;
                                case 1:
                                    spqcdSize = 16;
                                    scalarExpounded = false;
                                    break;
                                case 2:
                                    spqcdSize = 16;
                                    scalarExpounded = true;
                                    break;
                                default:
                                    throw new Exception("Invalid SQcd value " + sqcd);
                            }
                            qcd.noQuantization = spqcdSize == 8;
                            qcd.scalarExpounded = scalarExpounded;
                            qcd.guardBits = sqcd >> 5;
                            spqcds = new List<Spqcd>();
                            while (j < length + position)
                            {
                                var spqcd = new Spqcd();
                                if (spqcdSize == 8)
                                {
                                    spqcd.epsilon = data[j++] >> 3;
                                    spqcd.mu = 0;
                                }
                                else
                                {
                                    spqcd.epsilon = data[j] >> 3;
                                    spqcd.mu = ((data[j] & 0x7) << 8) | data[j + 1];
                                    j += 2;
                                }
                                spqcds.Add(spqcd);
                            }
                            qcd.SPqcds = spqcds;
                            if (context.mainHeader)
                            {
                                context.QCD = qcd;
                            }
                            else
                            {
                                context.currentTile.QCD = qcd;
                                context.currentTile.QCC = [];
                            }
                            break;
                        case 0xff5d: // Quantization component (QCC)
                            length = core_utils.readUint16(data, position);
                            var qcc = new Qcx();
                            j = position + 2;
                            ushort cqcc;
                            if (context.SIZ.Csiz < 257)
                            {
                                cqcc = data[j++];
                            }
                            else
                            {
                                cqcc = core_utils.readUint16(data, j);
                                j += 2;
                            }
                            sqcd = data[j++];
                            switch (sqcd & 0x1f)
                            {
                                case 0:
                                    spqcdSize = 8;
                                    scalarExpounded = true;
                                    break;
                                case 1:
                                    spqcdSize = 16;
                                    scalarExpounded = false;
                                    break;
                                case 2:
                                    spqcdSize = 16;
                                    scalarExpounded = true;
                                    break;
                                default:
                                    throw new Exception("Invalid SQcd value " + sqcd);
                            }
                            qcc.noQuantization = spqcdSize == 8;
                            qcc.scalarExpounded = scalarExpounded;
                            qcc.guardBits = sqcd >> 5;
                            spqcds = [];
                            while (j < length + position)
                            {
                                var spqcd = new Spqcd();
                                if (spqcdSize == 8)
                                {
                                    spqcd.epsilon = data[j++] >> 3;
                                    spqcd.mu = 0;
                                }
                                else
                                {
                                    spqcd.epsilon = data[j] >> 3;
                                    spqcd.mu = ((data[j] & 0x7) << 8) | data[j + 1];
                                    j += 2;
                                }
                                spqcds.Add(spqcd);
                            }
                            qcc.SPqcds = spqcds;
                            if (context.mainHeader)
                            {
                                context.QCC[cqcc] = qcc;
                            }
                            else
                            {
                                context.currentTile.QCC[cqcc] = qcc;
                            }
                            break;
                        case 0xff52: // Coding style default (COD)
                            length = core_utils.readUint16(data, position);
                            var cod = new Cod();
                            j = position + 2;
                            var scod = data[j++];
                            cod.entropyCoderWithCustomPrecincts = (scod & 1) != 0;
                            cod.sopMarkerUsed = (scod & 2) != 0;
                            cod.ephMarkerUsed = (scod & 4) != 0;
                            cod.progressionOrder = data[j++];
                            cod.layersCount = core_utils.readUint16(data, j);
                            j += 2;
                            cod.multipleComponentTransform = data[j++];

                            cod.decompositionLevelsCount = data[j++];
                            cod.xcb = (data[j++] & 0xf) + 2;
                            cod.ycb = (data[j++] & 0xf) + 2;
                            var blockStyle = data[j++];
                            cod.selectiveArithmeticCodingBypass = (blockStyle & 1) != 0;
                            cod.resetContextProbabilities = (blockStyle & 2) != 0;
                            cod.terminationOnEachCodingPass = (blockStyle & 4) != 0;
                            cod.verticallyStripe = (blockStyle & 8) != 0;
                            cod.predictableTermination = (blockStyle & 16) != 0;
                            cod.segmentationSymbolUsed = (blockStyle & 32) != 0;
                            cod.reversibleTransformation = data[j++];
                            if (cod.entropyCoderWithCustomPrecincts)
                            {
                                var precinctsSizes = new List<PrecinctsSize>();
                                while (j < length + position)
                                {
                                    var precinctsSize = data[j++];
                                    precinctsSizes.Add(new PrecinctsSize(precinctsSize & 0xf, precinctsSize >> 4));
                                }
                                cod.precinctsSizes = precinctsSizes;
                            }

                            List<string> unsupported = new List<string>();
                            if (cod.selectiveArithmeticCodingBypass)
                            {
                                unsupported.Add("selectiveArithmeticCodingBypass");
                            }
                            if (cod.terminationOnEachCodingPass)
                            {
                                unsupported.Add("terminationOnEachCodingPass");
                            }
                            if (cod.verticallyStripe)
                            {
                                unsupported.Add("verticallyStripe");
                            }
                            if (cod.predictableTermination)
                            {
                                unsupported.Add("predictableTermination");
                            }

                            if (unsupported.Count > 0)
                            {
                                doNotRecover = true;
                                System.Diagnostics.Debug.WriteLine($"warn: JPX: Unsupported COD options({string.Join(", ", unsupported)}).");
                            }

                            if (context.mainHeader)
                            {
                                context.COD = cod;
                            }
                            else
                            {
                                context.currentTile.COD = cod;
                                context.currentTile.COC = [];
                            }
                            break;
                        case 0xff90: // Start of tile-part (SOT)
                            length = core_utils.readUint16(data, position);
                            tile = new Tile();
                            tile.index = core_utils.readUint16(data, position + 2);
                            tile.length = core_utils.readUint32(data, position + 4);
                            tile.dataEnd = (uint)(tile.length + position - 2); // TODO - uint really?
                            tile.partIndex = data[position + 8];
                            tile.partsCount = data[position + 9];

                            context.mainHeader = false;
                            if (tile.partIndex == 0)
                            {
                                // reset component specific settings
                                tile.COD = context.COD;
                                tile.COC = context.COC.slice(0); // clone of the global COC
                                tile.QCD = context.QCD;
                                tile.QCC = context.QCC.slice(0); // clone of the global COC
                            }
                            context.currentTile = tile;
                            break;
                        case 0xff93: // Start of data (SOD)
                            tile = context.currentTile;
                            if (tile.partIndex == 0)
                            {
                                initializeTile(context, tile.index);
                                buildPackets(context);
                            }

                            // moving to the end of the data
                            length = Convert.ToInt32(tile.dataEnd - position); // TODO - Warning conversion
                            parseTilePackets(context, data, position, length);
                            break;
                        case 0xff53: // Coding style component (COC)
                                System.Diagnostics.Debug.WriteLine("warn: JPX: Codestream code 0xFF53 (COC) is not implemented.");
                                break;
                        /* falls through */
                        case 0xff55: // Tile-part lengths, main header (TLM)
                        case 0xff57: // Packet length, main header (PLM)
                        case 0xff58: // Packet length, tile-part header (PLT)
                        case 0xff64: // Comment (COM)
                            length = core_utils.readUint16(data, position);
                            // skipping content
                            break;
                        default:
                            throw new Exception("Unknown codestream code: " + code.ToString()); // toString(16));
                        }
                        position += length;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            void calculateComponentDimensions(Component component, Siz siz)
            {
                // Section B.2 Component mapping
                component.x0 = (int)Math.Ceiling(siz.XOsiz / (double)component.XRsiz); // TODO - careful casting to int?
                component.x1 = (int)Math.Ceiling(siz.Xsiz / (double)component.XRsiz); // TODO - careful casting to int?
                component.y0 = (int)Math.Ceiling(siz.YOsiz / (double)component.YRsiz); // TODO - careful casting to int?
                component.y1 = (int)Math.Ceiling(siz.Ysiz / (double)component.YRsiz); // TODO - careful casting to int?
                component.width = component.x1 - component.x0;
                component.height = component.y1 - component.y0;
            }

            void calculateTileGrids(Context context, IReadOnlyList<Component> components)
            {
                var siz = context.SIZ;
                // Section B.3 Division into tile and tile-components
                List<Tile> tiles = [];
                Tile tile;
                int numXtiles = (int)Math.Ceiling((siz.Xsiz - siz.XTOsiz) / (double)siz.XTsiz);
                int numYtiles = (int)Math.Ceiling((siz.Ysiz - siz.YTOsiz) / (double)siz.YTsiz);
                for (var q = 0; q < numYtiles; q++)
                {
                    for (var p = 0; p < numXtiles; p++)
                    {
                        tile = new Tile();
                        tile.tx0 = (int)Math.Max(siz.XTOsiz + p * siz.XTsiz, siz.XOsiz); // TODO - int?
                        tile.ty0 = (int)Math.Max(siz.YTOsiz + q * siz.YTsiz, siz.YOsiz);
                        tile.tx1 = (int)Math.Max(siz.XTOsiz + (p + 1) * siz.XTsiz, siz.Xsiz);
                        tile.ty1 = (int)Math.Max(siz.YTOsiz + (q + 1) * siz.YTsiz, siz.Ysiz);
                        tile.width = tile.tx1 - tile.tx0;
                        tile.height = tile.ty1 - tile.ty0;
                        tile.components = [];
                        tiles.Add(tile);
                    }
                }
                context.tiles = tiles;

                var componentsCount = siz.Csiz;
                var ii = componentsCount;
                for (var i = 0; i < ii; i++)
                {
                    var component = components[i];
                    var jj = tiles.Count;
                    for (var j = 0; j < jj; j++)
                    {
                        var tileComponent = new Component();
                        tile = tiles[j];
                        tileComponent.tcx0 = (int)Math.Ceiling(tile.tx0 / (double)component.XRsiz); // TODO - cast to int?
                        tileComponent.tcy0 = (int)Math.Ceiling(tile.ty0 / (double)component.YRsiz);
                        tileComponent.tcx1 = (int)Math.Ceiling(tile.tx1 / (double)component.XRsiz);
                        tileComponent.tcy1 = (int)Math.Ceiling(tile.ty1 / (double)component.YRsiz);
                        tileComponent.width = tileComponent.tcx1 - tileComponent.tcx0;
                        tileComponent.height = tileComponent.tcy1 - tileComponent.tcy0;
                        tile.components[i] = tileComponent;
                    }
                }
            }

            object getBlocksDimensions( Context context, Component component, int r)
            {
                const codOrCoc = component.codingStyleParameters;
                const result = { };
                if (!codOrCoc.entropyCoderWithCustomPrecincts)
                {
                    result.PPx = 15;
                    result.PPy = 15;
                }
                else
                {
                    result.PPx = codOrCoc.precinctsSizes[r].PPx;
                    result.PPy = codOrCoc.precinctsSizes[r].PPy;
                }
                // calculate codeblock size as described in section B.7
                result.xcb_ =
                    r > 0
                        ? Math.Min(codOrCoc.xcb, result.PPx - 1)
                        : Math.Min(codOrCoc.xcb, result.PPx);
                result.ycb_ =
                    r > 0
                        ? Math.Min(codOrCoc.ycb, result.PPy - 1)
                        : Math.Min(codOrCoc.ycb, result.PPy);
                return result;
            }

            void buildPrecincts(Context context, resolution, PrecinctsSize dimensions) // TODO - Check if PrecinctsSize is dimensions
            {
                // Section B.6 Division resolution to precincts
                var precinctWidth = 1 << dimensions.PPx;
                var precinctHeight = 1 << dimensions.PPy;
                // Jasper introduces codeblock groups for mapping each subband codeblocks
                // to precincts. Precinct partition divides a resolution according to width
                // and height parameters. The subband that belongs to the resolution level
                // has a different size than the level, unless it is the zero resolution.

                // From Jasper documentation: jpeg2000.pdf, section K: Tier-2 coding:
                // The precinct partitioning for a particular subband is derived from a
                // partitioning of its parent LL band (i.e., the LL band at the next higher
                // resolution level)... The LL band associated with each resolution level is
                // divided into precincts... Each of the resulting precinct regions is then
                // mapped into its child subbands (if any) at the next lower resolution
                // level. This is accomplished by using the coordinate transformation
                // (u, v) = (ceil(x/2), ceil(y/2)) where (x, y) and (u, v) are the
                // coordinates of a point in the LL band and child subband, respectively.
                var isZeroRes = resolution.resLevel == 0;
                var precinctWidthInSubband = 1 << (dimensions.PPx + (isZeroRes ? 0 : -1));
                var precinctHeightInSubband = 1 << (dimensions.PPy + (isZeroRes ? 0 : -1));
                var numprecinctswide =
                  resolution.trx1 > resolution.trx0
                    ? Math.ceil(resolution.trx1 / precinctWidth) -
                      Math.floor(resolution.trx0 / precinctWidth)
                    : 0;
                const numprecinctshigh =
                  resolution.try1 > resolution.try0
                    ? Math.ceil(resolution.try1 / precinctHeight) -
                      Math.floor(resolution.try0 / precinctHeight)
                    : 0;
                const numprecincts = numprecinctswide * numprecinctshigh;

                resolution.precinctParameters = {
                    precinctWidth,
                    precinctHeight,
                    numprecinctswide,
                    numprecinctshigh,
                    numprecincts,
                    precinctWidthInSubband,
                    precinctHeightInSubband,

                };
            }


        }
    }
}
