"""Shows that a form XObject's stream is re-read and re-parsed on every `Do`
(README, cause B, second effect). The page invokes one form NDO times; PAD controls how
large the form's content stream is.

    python make-large-form-pdf.py <NDO> <PAD> <out.pdf>

PAD=0 gives a 15-byte form, PAD=4000 gives a ~196 KB form; the difference in allocated
bytes across the two runs is the per-invocation re-read.
"""
import sys
NDO = int(sys.argv[1]); PAD = int(sys.argv[2]); OUT = sys.argv[3]
def stream_obj(extra, data):
    return b"<< " + extra + b" /Length " + str(len(data)).encode() + b" >>\nstream\n" + data + b"\nendstream"
objs = {}
objs[1] = b"<< /Type /Catalog /Pages 2 0 R >>"
objs[2] = b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>"
objs[3] = b"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources 4 0 R /Contents 5 0 R >>"
objs[4] = b"<< /XObject << /Fm0 6 0 R >> >>"
objs[5] = stream_obj(b"", b"/Fm0 Do\n" * NDO)
form = b"0 0 10 10 re f\n" + (b"% pad comment line to make the form stream large\n" * PAD)
objs[6] = stream_obj(b"/Type /XObject /Subtype /Form /BBox [0 0 10 10] /Resources << >>", form)
maxnum = max(objs)
out = bytearray(b"%PDF-1.7\n%\xe2\xe3\xcf\xd3\n"); offsets = {}
for n in sorted(objs):
    offsets[n] = len(out); out += b"%d 0 obj\n" % n + objs[n] + b"\nendobj\n"
xp = len(out)
out += b"xref\n0 %d\n" % (maxnum+1) + b"0000000000 65535 f \n"
for n in range(1, maxnum+1): out += b"%010d 00000 n \n" % offsets[n]
out += b"trailer\n<< /Size %d /Root 1 0 R >>\nstartxref\n%d\n%%%%EOF\n" % (maxnum+1, xp)
open(OUT,"wb").write(bytes(out)); print("wrote", OUT, len(out), "form stream bytes:", len(form))
