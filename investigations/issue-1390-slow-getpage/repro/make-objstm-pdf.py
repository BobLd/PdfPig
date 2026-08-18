"""Control document for README cause B: the same logical PDF as
`make-classic-xref-pdf.py <NGS> <NDO> out.pdf xobject`, but with the shared resource
dictionary and its large sub-dictionary stored inside an *object stream* addressed by an
xref *stream*.

That is the one path where PdfTokenScanner calls IObjectLocationProvider.Cache, so repeated
resolution of the same indirect reference is served from memory. Comparing this document
against the classic-xref one isolates the cost of the missing cache.

    python make-objstm-pdf.py <NGS> <NDO> <out.pdf>
"""
import sys, zlib
NGS = int(sys.argv[1]); NDO = int(sys.argv[2]); OUT = sys.argv[3]
# objects 7 (form resources) and 8 (big XObject dict) live inside an object stream -> scanner caches them
OBJSTM = 9
XREFSTM = 10
MAXNUM = 10

def stream_obj(extra, data):
    return b"<< " + extra + b" /Length " + str(len(data)).encode() + b" >>\nstream\n" + data + b"\nendstream"

regular = {}
regular[1] = b"<< /Type /Catalog /Pages 2 0 R >>"
regular[2] = b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>"
regular[3] = b"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources 4 0 R /Contents 5 0 R >>"
regular[4] = b"<< /XObject << /Fm0 6 0 R >> >>"
regular[5] = stream_obj(b"", b"/Fm0 Do\n" * NDO)
regular[6] = stream_obj(b"/Type /XObject /Subtype /Form /BBox [0 0 10 10] /Resources 7 0 R", b"0 0 10 10 re f\n")

o7 = b"<< /XObject 8 0 R >>"
o8 = b"<< " + b"".join(b"/X%d 6 0 R " % i for i in range(NGS)) + b">>"
pairs = [(7, o7), (8, o8)]
header = b""; body = b""
for num, data in pairs:
    header += b"%d %d " % (num, len(body))
    body += data + b" "
objstm_payload = header + body
first = len(header)
comp = zlib.compress(objstm_payload)
regular[OBJSTM] = stream_obj(b"/Type /ObjStm /N %d /First %d /Filter /FlateDecode" % (len(pairs), first), comp)

out = bytearray(b"%PDF-1.7\n%\xe2\xe3\xcf\xd3\n")
offsets = {}
for n in sorted(regular):
    offsets[n] = len(out)
    out += b"%d 0 obj\n" % n + regular[n] + b"\nendobj\n"

xref_pos = len(out)
# xref stream: W [1 4 2]
rows = bytearray()
def row(t, f2, f3):
    rows.extend(bytes([t]) + f2.to_bytes(4, 'big') + f3.to_bytes(2, 'big'))
row(0, 0, 65535)
for n in range(1, MAXNUM + 1):
    if n in offsets: row(1, offsets[n], 0)
    elif n == 7: row(2, OBJSTM, 0)
    elif n == 8: row(2, OBJSTM, 1)
    elif n == XREFSTM: row(1, xref_pos, 0)
    else: row(0, 0, 65535)
xcomp = zlib.compress(bytes(rows))
xdict = b"/Type /XRef /Size %d /W [1 4 2] /Root 1 0 R /Filter /FlateDecode" % (MAXNUM + 1)
out += b"%d 0 obj\n" % XREFSTM + stream_obj(xdict, xcomp) + b"\nendobj\n"
out += b"startxref\n%d\n%%%%EOF\n" % xref_pos
open(OUT, "wb").write(bytes(out))
print("wrote", OUT, len(out))
