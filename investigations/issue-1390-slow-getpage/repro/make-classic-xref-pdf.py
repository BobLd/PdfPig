"""Generates a single-page PDF that reproduces UglyToad/PdfPig#1390, using a classic
xref *table* (so PdfTokenScanner's object cache is never populated -- see README, cause B).

The page contains NDO `/Fm0 Do` operations. The form XObject's /Resources is an indirect
reference to one shared dictionary holding NGS entries under the key selected by MODE.

    python make-classic-xref-pdf.py <NGS> <NDO> <out.pdf> <MODE>

MODE is one of:
    none            form resources are empty            (baseline)
    extg-indirect   /ExtGState, values are `n 0 R`       (the shape reported in #1390)
    extg-direct     /ExtGState, values are inline dicts  (isolates indirect resolution)
    xobject         /XObject,   values are `n 0 R`
    properties      /Properties, values are inline dicts
    colorspace      /ColorSpace, values are /DeviceRGB names
"""
import sys

NGS  = int(sys.argv[1]); NDO = int(sys.argv[2]); OUT = sys.argv[3]; MODE = sys.argv[4]

objs = {}
def stream_obj(extra, data):
    return b"<< " + extra + b" /Length " + str(len(data)).encode() + b" >>\nstream\n" + data + b"\nendstream"

GS_START = 9
objs[1] = b"<< /Type /Catalog /Pages 2 0 R >>"
objs[2] = b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>"
objs[3] = b"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources 4 0 R /Contents 5 0 R >>"
objs[4] = b"<< /XObject << /Fm0 6 0 R >> >>"
objs[5] = stream_obj(b"", b"/Fm0 Do\n" * NDO)
objs[6] = stream_obj(b"/Type /XObject /Subtype /Form /BBox [0 0 10 10] /Resources 7 0 R", b"0 0 10 10 re f\n")

if MODE == "extg-indirect":
    objs[7] = b"<< /ExtGState 8 0 R >>"
    objs[8] = b"<< " + b"".join(b"/G%d %d 0 R " % (i, GS_START + i) for i in range(NGS)) + b">>"
    for i in range(NGS):
        objs[GS_START + i] = b"<< /Type /ExtGState /LW %d >>" % ((i % 7) + 1)
elif MODE == "extg-direct":
    objs[7] = b"<< /ExtGState 8 0 R >>"
    objs[8] = b"<< " + b"".join(b"/G%d << /Type /ExtGState /LW %d >> " % (i, (i % 7) + 1) for i in range(NGS)) + b">>"
elif MODE == "xobject":
    objs[7] = b"<< /XObject 8 0 R >>"
    objs[8] = b"<< " + b"".join(b"/X%d 6 0 R " % i for i in range(NGS)) + b">>"
elif MODE == "properties":
    objs[7] = b"<< /Properties 8 0 R >>"
    objs[8] = b"<< " + b"".join(b"/P%d << /MCID %d >> " % (i, i) for i in range(NGS)) + b">>"
elif MODE == "colorspace":
    objs[7] = b"<< /ColorSpace 8 0 R >>"
    objs[8] = b"<< " + b"".join(b"/C%d /DeviceRGB " % i for i in range(NGS)) + b">>"
elif MODE == "none":
    objs[7] = b"<< >>"
else:
    raise SystemExit("bad mode")

maxnum = max(objs)
out = bytearray(b"%PDF-1.7\n%\xe2\xe3\xcf\xd3\n")
offsets = {}
for n in sorted(objs):
    offsets[n] = len(out)
    out += b"%d 0 obj\n" % n + objs[n] + b"\nendobj\n"
xref_pos = len(out)
out += b"xref\n0 %d\n" % (maxnum + 1) + b"0000000000 65535 f \n"
for n in range(1, maxnum + 1):
    out += (b"%010d 00000 n \n" % offsets[n]) if n in offsets else b"0000000000 65535 f \n"
out += b"trailer\n<< /Size %d /Root 1 0 R >>\nstartxref\n%d\n%%%%EOF\n" % (maxnum + 1, xref_pos)
open(OUT, "wb").write(bytes(out))
print("wrote", OUT, MODE, len(out))
