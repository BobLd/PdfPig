# Is each part of the #1390 fix worth its complexity?

The fix for [#1390](https://github.com/UglyToad/PdfPig/issues/1390) landed as three separable
pieces of very different size. This is a measured answer to whether the two larger ones earn
their keep, or whether the twelve-line scanner change would have been enough.

Short answer: **the scanner cache alone is not enough — it leaves the reported document at 16
seconds.** Only the resource-dictionary memoization brings it under a second, and it is the one
piece that costs something measurable on ordinary documents. The form-stream cache does nothing
for #1390 at all; it fixes a different pathology and carries the only breaking API change.

## Variants compared

| variant | contents | production diff |
| --- | --- | --- |
| `baseline` | `755c1087`, before any of the work | — |
| `scanner-only` | baseline + `PdfTokenScanner.cs` only (cause B) | **+12 / −0, 1 file** |
| `scanner+form` | baseline + cause B + the form-stream cache, **no** cause A | +98 / −5, 5 files |
| `a-plus-b` | `6921ae74` — causes A and B | +216 / −47, 4 files |
| `full` | `4d50e50d` — A, B and the form-stream cache | +279 / −52, 6 files |

`scanner+form` does not correspond to a commit. It was constructed to isolate cause A, by taking
`PdfTokenScanner.cs` and `ReferenceEqualityComparer.cs` from `6921ae74`, `BaseStreamProcessor.cs`
from `4d50e50d`, and adding only `TryGetXObjectReference` to the baseline `ResourceStore` —
leaving out the memoization and the `StackDictionary` rework entirely.

## Method

Each variant was published side by side from a throwaway clone, then measured round-robin so
that drift in machine state is shared between variants rather than attributed to one of them.
Each process runs the workload three times and the first (cold, JIT-dominated) repetition is
discarded; the statistic is the minimum of the warm runs, and three such rounds were taken.

Two workloads:

- **the 13 reproduction documents** in `documents/` — `Open` + `GetPage(1)`
- **the real test corpus** — all 150 PDFs in `src/UglyToad.PdfPig.Tests/Integration/Documents`,
  `Open` + up to 5 pages each. Every document succeeded in every variant.

Measurements: `Stopwatch`, `GC.GetTotalAllocatedBytes(precise: true)`, GC collection counts.
.NET 8, Release, Windows 11. All raw measurements and the scripts that produced them are in
`benchmark/`.

### What this method can and cannot resolve

This matters for reading the corpus numbers, so it is stated up front rather than buried.

The corpus **total** is not a reliable discriminator. Round-to-round spread of the total within a
single variant reaches 11.6%, while the spread of the best totals *across* all five variants is
only 6.6%. The between-variant differences are smaller than the within-variant noise, so any
claim of the form "variant X is 3% faster overall" is unsupported.

| variant | round 1 | round 2 | round 3 | min | within-variant spread |
| --- | --- | --- | --- | --- | --- |
| `baseline` | 2020 | 2026 | 2130 | 2020 | 5.4% |
| `scanner-only` | 1994 | 2121 | 1962 | 1962 | 8.1% |
| `scanner+form` | 1972 | 2114 | 1895 | 1895 | 11.6% |
| `a-plus-b` | 2078 | 2070 | 1902 | 1902 | 9.2% |
| `full` | 2003 | 2075 | 2118 | 2003 | 5.7% |

A **paired per-document sign test** is far more sensitive, because pairing cancels the
round-level drift that swamps the totals. That is what the ordinary-document conclusions below
rest on. An earlier interleaved attempt that used one repetition per process is discarded
entirely — every measurement paid JIT, giving a median per-document spread of 285%.

## Result 1 — the reported pathology

`fullscale-extgstate-30k-4200do.pdf`: one page, ~4 200 form-XObject invocations sharing one
resource dictionary with a 30 000-entry `/ExtGState`. `Open` + `GetPage(1)`:

| variant | time | allocated | vs baseline |
| --- | --- | --- | --- |
| `baseline` | 248 634 ms | 192 600 MB | 1× |
| `scanner-only` | 16 182 ms | 7 958 MB | 15× |
| `scanner+form` | 15 947 ms | 7 943 MB | 16× |
| `a-plus-b` | **145 ms** | 107 MB | **1 715×** |
| `full` | **131 ms** | 93 MB | **1 898×** |

This is the decisive result. The scanner cache is a 15× win, but **it leaves the document at 16
seconds** — still "GetPage takes forever" by any reasonable standard. Cause A supplies a further
**110×** on top, and it is the only piece that does. The same shape holds across every
`isolate-*` document: `scanner-only` lands in the 600–1 000 ms range, `a-plus-b` in the 20–90 ms
range.

The form-stream cache contributes nothing here (`a-plus-b` → `full` is 145 → 131 ms, inside
noise), because this document's form is 15 bytes. It is aimed at a different pathology:

`formstream-196kb-200do.pdf` — 200 invocations of one 196 KB form:

| variant | time | allocated |
| --- | --- | --- |
| `baseline` | 209 ms | 149.8 MB |
| `scanner-only` | 205 ms | 149.8 MB |
| `a-plus-b` | 235 ms | 149.8 MB |
| `scanner+form` | **7.9 ms** | **2.5 MB** |
| `full` | **11.5 ms** | **2.4 MB** |

Only the form-stream cache moves this, and it moves it 18× in time and 60× in allocation.
Neither B nor A touches it — as expected, since excluding streams from the object cache is
exactly what leaves the re-read in place.

## Result 2 — ordinary documents (150-document corpus)

Paired sign test over the 67 corpus documents whose baseline exceeds 5 ms (below that, timer
resolution dominates). "faster"/"slower" count documents, `p` is a two-sided binomial test:

| comparison | faster | slower | median ratio | p |
| --- | --- | --- | --- | --- |
| `scanner-only` vs `baseline` | 36 | 31 | 0.984 | 0.63 |
| `scanner+form` vs `baseline` | 33 | 34 | 1.000 | 1.00 |
| `a-plus-b` vs `baseline` | 31 | 36 | 1.000 | 0.63 |
| `full` vs `baseline` | 26 | 41 | 1.010 | 0.09 |
| `a-plus-b` vs `scanner-only` (adds A) | 30 | 37 | 1.012 | 0.46 |
| **`full` vs `scanner+form` (adds A)** | **19** | **48** | **1.024** | **0.001** |
| `scanner+form` vs `scanner-only` (adds form cache) | 33 | 34 | 1.000 | 1.00 |
| `full` vs `a-plus-b` (adds form cache) | 24 | 43 | 1.016 | 0.03 |

Reading this honestly:

- **No variant delivers a measurable speedup on ordinary documents.** Every comparison against
  baseline is non-significant, with median ratios between 0.98 and 1.01. The pathology being
  fixed simply does not occur in these files.
- **Cause A carries a small but real cost.** Both comparisons that add it point the same way
  (median 1.024 and 1.012), and one is significant at p = 0.001. Best estimate: **ordinary
  documents get about 1–2.5% slower.**
- The two comparisons that add the form-stream cache disagree (p = 1.00 and p = 0.03) and are
  best read as **no effect** on ordinary documents.

Allocation is consistent and small, and is worth separating from time:

| variant | corpus allocation | vs baseline |
| --- | --- | --- |
| `baseline` | 1 976 MB | 100.0% |
| `scanner-only` | 1 932 MB | 97.8% |
| `scanner+form` | 1 929 MB | 97.6% |
| `a-plus-b` | 1 932 MB | 97.8% |
| `full` | 1 929 MB | 97.6% |

All four fixed variants sit at the same 97.6–97.8%. **The entire ordinary-document allocation
win — such as it is — comes from the scanner cache**; A and the form cache add nothing to it.

### Why cause A costs something

`StackDictionary.TryGetValue` is one of the hottest paths in the library: every font, XObject,
colour-space and ExtGState lookup walks it. Cause A changed its backing store from
`List<Dictionary<K,V>>` to `List<Level>`, where `Level` is a 16-byte struct, so each level probe
now copies a struct instead of loading a reference. That allocation figures are *identical*
across variants rules out the extra `ResolvedResources` objects as the cause and points at the
lookup path. This is plausibly recoverable — see the recommendation.

## Result 3 — memory retained

Allocation is throughput; retention is the price the caches actually charge. Measured as
`GC.GetTotalMemory(precise: true)` **while the document is still open**, so everything the caches
hold is still rooted, one process per document.

Retained managed heap, MB:

| document | pages | `baseline` | `scanner-only` | `scanner+form` | `a-plus-b` | `full` |
| --- | --- | --- | --- | --- | --- | --- |
| `fullscale-extgstate-30k-4200do.pdf` | 1 | 15.0 | 26.6 | 26.6 | 27.8 | 27.8 |
| `formstream-196kb-200do.pdf` | 1 | 3.6 | 3.6 | 3.6 | 3.7 | 3.6 |
| `0000851.pdf` | 5 | 25.6 | 25.7 | 25.7 | 25.8 | 25.8 |
| `MOZILLA-3136-0.pdf` | 4 | 53.4 | 53.4 | 53.4 | 53.6 | 53.7 |
| `2108.11480.pdf` | 5 | 20.3 | 20.6 | 20.6 | 20.6 | 20.6 |

Peak working set, MB:

| document | `baseline` | `scanner-only` | `scanner+form` | `a-plus-b` | `full` |
| --- | --- | --- | --- | --- | --- |
| `fullscale-extgstate-30k-4200do.pdf` | 189.0 | 104.2 | 103.7 | 136.2 | 121.5 |
| `formstream-196kb-200do.pdf` | 65.3 | 65.0 | 57.1 | 65.1 | 57.2 |
| `0000851.pdf` | 116.7 | 115.3 | 115.2 | 115.2 | 115.3 |
| `MOZILLA-3136-0.pdf` | 214.8 | 214.8 | 214.9 | 210.9 | 214.9 |
| `2108.11480.pdf` | 105.3 | 105.8 | 104.3 | 105.2 | 104.9 |

On ordinary documents the caches retain **+0.2 to +0.3 MB** — negligible, and cause A adds at
most 0.1 MB of that. The retention cost concentrates exactly where the caching does work: on the
pathological document the scanner cache holds an extra 11.6 MB of tokens and cause A a further
1.2 MB.

Peak working set moves the other way, and decisively: 189 MB → 104–136 MB on the pathological
document, because 188 GB of garbage is no longer being produced and collected. So the caches
trade a small, bounded increase in *retained* memory for a large reduction in *peak* memory. The
one caveat this does not measure is a long-lived document with very many distinct resource
dictionaries, where cause A's per-document cache is unbounded; 25 pages of `MOZILLA-3136-0.pdf`
cost 0.3 MB, but that is a sample, not a bound.

## The cost side

| | `scanner-only` | cause A | form-stream cache |
| --- | --- | --- | --- |
| production lines | +12 | +204 / −47 | +63 / −5 |
| files touched | 1 | 4 | 3 |
| new types | 0 | `ReferenceEqualityComparer<T>`, `ResolvedResources`, `StackDictionary.Level` | 0 |
| new public API | none | none | `IResourceStore.TryGetXObjectReference` (**source-breaking**), `BaseStreamProcessor.GetFormOperations` |
| new invariants to hold | none | cached levels must not be mutated (copy-on-write); `/Pattern` and `/Shading` must stay uncached because they resolve through the resource stack | form cache must hold forms only, never images, or it pins image bytes for the page |
| lifetime of new state | none | per document, unbounded | per page |

The scanner change is a guarded call inside one method. Cause A is a restructuring of
`ResourceStore` plus a change to a data structure on a hot path, and it introduces the subtlest
invariant in the whole fix: the pattern/shading exclusion is a correctness requirement that is
not obvious from the code and is easy for a future change to violate.

## Verdict

**Cause B (scanner cache) — unambiguously worth it.** Twelve lines, no new types, no API change,
no measurable cost anywhere, 15× on the pathological document and the only source of the
corpus-wide allocation reduction. It would be worth taking even on its own.

**Cause B alone is not sufficient.** It leaves the reported document at 16 seconds. If the bar is
"the document from #1390 opens in reasonable time", the scanner change does not clear it.

**Cause A — worth it, but it is the one that has to justify itself.** It is solely responsible for
the last 110×, taking the reported document from 16 s to 0.14 s. Against that it costs roughly
1–2.5% on ordinary documents (p = 0.001 on the cleanest comparison), adds ~200 lines, and
introduces an unbounded per-document cache and a non-obvious correctness invariant. For a library
whose job is to not fall over on adversarial real-world PDFs, trading ~2% on typical files for
three orders of magnitude on a pathological one is a good trade — but it is a trade, not a free
win, and it should be described that way.

**Form-stream cache — worth it on its own merits, not as part of #1390.** It contributes nothing
to the reported document and nothing measurable to the corpus. It fixes a genuine, separate
O(invocations × stream size) defect, worth 18× on a document with large repeatedly-invoked forms.
It also carries the only breaking API change in the whole fix. It would be entirely reasonable to
land it as a separate change, judged on its own, rather than folded into #1390.

## Recommended follow-up

Cause A's ~2% cost looks recoverable, and doing so would remove the only real objection to it:

1. **Undo the hot-path regression.** Keep `values` as `List<Dictionary<K,V>>` and track the
   shared/owned flag in a parallel structure, so `TryGetValue` goes back to loading a reference
   instead of copying a 16-byte struct. Measure with the paired sign test above — it is sensitive
   enough to detect a 2% shift, which the totals are not.
2. **Consider memoizing lazily**, on the second load of a given resource dictionary rather than
   the first, so documents that load each dictionary once pay nothing and never populate the
   cache. That also bounds the per-document cache to dictionaries that are actually reused.

If step 1 lands, cause A becomes close to free on ordinary documents and the trade disappears.
