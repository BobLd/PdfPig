"""Builds the 'scanner+form' variant: baseline + the scanner cache + the form-stream fix, but
without cause A's resource-dictionary memoization. Run inside the bench clone checked out at baseline
with PdfTokenScanner.cs, BaseStreamProcessor.cs and ReferenceEqualityComparer.cs already taken from
the later commits."""
import sys, io

clone = sys.argv[1]

# 1. IResourceStore: add the reference lookup the form cache needs.
p = clone + "/src/UglyToad.PdfPig/Content/IResourceStore.cs"
s = open(p, encoding="utf-8-sig").read()
assert "TryGetXObjectReference" not in s
s = s.replace("    using Graphics.Colors;", "    using Core;\n    using Graphics.Colors;", 1)
anchor = "        bool TryGetXObject(NameToken name, [NotNullWhen(true)] out StreamToken? stream);"
assert anchor in s
s = s.replace(anchor, anchor + """

        /// <summary>
        /// Try getting the reference of the XObject corresponding to the name, without resolving the object
        /// it points at.
        /// </summary>
        bool TryGetXObjectReference(NameToken name, out IndirectReference reference);""", 1)
open(p, "w", encoding="utf-8-sig").write(s)

# 2. ResourceStore: implement it. Nothing else from cause A is taken.
p = clone + "/src/UglyToad.PdfPig/Content/ResourceStore.cs"
s = open(p, encoding="utf-8-sig").read()
assert "TryGetXObjectReference" not in s
assert "resolvedResources" not in s, "expected the baseline ResourceStore, not the memoizing one"
anchor = """            return DirectObjectFinder.TryGet(new IndirectReferenceToken(indirectReference), scanner, out stream);
        }"""
assert anchor in s
s = s.replace(anchor, anchor + """

        public bool TryGetXObjectReference(NameToken name, out IndirectReference reference)
        {
            return currentXObjectState.TryGetValue(name, out reference);
        }""", 1)
open(p, "w", encoding="utf-8-sig").write(s)

print("scanner+form variant patched")
