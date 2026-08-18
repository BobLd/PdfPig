# Is each part of the #1390 fix worth its complexity?

The fix for [#1390](https://github.com/UglyToad/PdfPig/issues/1390) landed as three separable
pieces of very different size. This is a measured answer to whether the two larger ones earn
their keep, or whether the twelve-line scanner change would have been enough.

Short answer: **the scanner cache alone is not enough — it leaves the reported document at 15
seconds.** Only the resource-dictionary memoization brings it under a second, and once the
measurement is done properly **it costs nothing detectable on ordinary documents**. The
form-stream cache does nothing for #1390 at all; it fixes a different pathology and carries the
only breaking API change.

> **Correction.** An earlier revision of this document reported that the resource-dictionary
> memoization made ordinary documents 1–2.5% slower (p = 0.001). That was an artifact of running
> the variants in a fixed order within each measurement round: whichever variant ran last was
> systematically penalised. With the order rotated the effect disappears (p = 0.33). The claim is
> withdrawn, and the `StackDictionary` change that was recommended to fix it turned out to have
> nothing to fix — it was implemented, measured, and reverted. See
> [Measurement validity](#measurement-validity).

## Variants compared

| variant | contents | production diff |
| --- | --- | --- |
| `baseline` | `755c1087`, before any of the work | — |
| `scanner-only` | baseline + `PdfTokenScanner.cs` only (cause B) | **+12 / −0, 1 file** |
| `scanner+form` | baseline + cause B + the form-stream cache, **no** cause A | +98 / −5, 5 files |
| `a-plus-b` | `6921ae74` — causes A and B | +216 / −47, 4 files |
| `full` | `4d50e50d` — A, B and the form-stream cache | +279 / −52, 6 files |
| `full-v2` | `full` + the `StackDictionary` hot-path change (working tree) | +289 / −57, 6 files |

`scanner+form` does not correspond to a commit. It was constructed to isolate cause A, by taking
`PdfTokenScanner.cs` and `ReferenceEqualityComparer.cs` from `6921ae74`, `BaseStreamProcessor.cs`
from `4d50e50d`, and adding only `TryGetXObjectReference` to the baseline `ResourceStore` —
leaving out the memoization and the `StackDictionary` rework entirely.

## Method

Each variant was published side by side from a throwaway clone, then measured round-robin so
that drift in machine state is shared between variants rather than attributed to one of them.
Each process runs the workload three times and the first (cold, JIT-dominated) repetition is
discarded; the statistic is the minimum of the warm runs. The order of variants is rotated every
round — see [Measurement validity](#measurement-validity), which is not an aside but the reason
one earlier conclusion had to be withdrawn.

Two workloads:

- **the 13 reproduction documents** in `documents/` — `Open` + `GetPage(1)`
- **the real test corpus** — the PDFs in `src/UglyToad.PdfPig.Tests/Integration/Documents`,
  `Open` + up to 5 pages each. Every document succeeded in every variant. Earlier sweeps used all
  150; the final rotated sweep uses the 69 whose baseline exceeds 5 ms, since the rest only
  measure timer resolution.

Measurements: `Stopwatch`, `GC.GetTotalAllocatedBytes(precise: true)`, GC collection counts.
.NET 8, Release, Windows 11. All raw measurements and the scripts that produced them are in
`benchmark/`.

### Measurement validity

Three successive methodological faults were found and fixed while producing this document. They
are recorded because each one produced a confident-looking wrong answer.

1. **Cold JIT.** An interleaved sweep with one repetition per process paid JIT on every
   measurement — median per-document spread 285%. Discarded entirely; runs now do three
   repetitions per process and drop the first.
2. **Drift between sweeps.** Variants measured minutes apart drift with machine state, which made
   `a-plus-b` look faster than baseline in one sweep and slower in another. Fixed by measuring all
   variants round-robin from side-by-side builds.
3. **Fixed ordering within a round.** Round-robin is not enough if the order inside each round is
   constant: the variant that runs last is systematically penalised. This is what produced the
   retracted "cause A costs 1–2.5%, p = 0.001" result — cause A's variant happened to run last.
   The tell was that when a later sweep appended a new variant after it, the penalty transferred
   to the newcomer and cause A's disappeared. Fixed by rotating the order every round so each
   variant occupies each position equally.

The numbers below come from the rotated design: 4 variants x 8 rounds x 3 repetitions, over the
69 corpus documents whose baseline exceeds 5 ms.

### What this method can and cannot resolve

This matters for reading the corpus numbers, so it is stated up front rather than buried.

The corpus **total** is not a reliable discriminator. Round-to-round spread of the total within a
single variant reaches 29.6%, while the spread of the best totals *across* the variants is only
6.5%. The between-variant differences are far smaller than the within-variant noise, so any claim
of the form "variant X is 3% faster overall" is unsupported. Rotated design, best of 8 rounds, ms:

| variant | min total | within-variant spread across rounds |
| --- | --- | --- |
| `baseline` | 1914 | 28.7% |
| `scanner+form` | 1843 | 29.6% |
| `full` | 1798 | 20.6% |
| `full-v2` | 1854 | 21.6% |

A **paired per-document sign test** is far more sensitive, because pairing cancels the
round-level drift that swamps the totals. That is what the ordinary-document conclusions below
rest on. Note that pairing cancels drift but **not** a fixed ordering bias, which is why fault 3
above survived until the order was rotated.

## Result 1 — the reported pathology

`fullscale-extgstate-30k-4200do.pdf`: one page, ~4 200 form-XObject invocations sharing one
resource dictionary with a 30 000-entry `/ExtGState`. `Open` + `GetPage(1)`:

| variant | time | allocated | vs baseline |
| --- | --- | --- | --- |
| `baseline` | 248 634 ms | 192 600 MB | 1× |
| `scanner-only` | 16 182 ms | 7 958 MB | 15× |
| `scanner+form` | 14 704 ms | 7 948 MB | 17× |
| `a-plus-b` | **145 ms** | 107 MB | **1 715×** |
| `full` | **110 ms** | 93 MB | **2 270×** |
| `full-v2` | **107 ms** | 93 MB | **2 324×** |

This is the decisive result, and it is the one place where the differences are far larger than
any measurement problem. The scanner cache is a 15× win, but **it leaves the document at ~15
seconds** — still "GetPage takes forever" by any reasonable standard. Cause A supplies a further
**~135×** on top, and it is the only piece that does. The same shape holds across every
`isolate-*` document: `scanner+form` lands in the 300–950 ms range, `full` in the 13–82 ms range.

The form-stream cache contributes nothing here (`a-plus-b` → `full` is 145 → 110 ms, within the
spread of these runs), because this document's form is 15 bytes. It is aimed at a different
pathology:

`formstream-196kb-200do.pdf` — 200 invocations of one 196 KB form:

| variant | time | allocated |
| --- | --- | --- |
| `baseline` | 209 ms | 149.8 MB |
| `scanner-only` | 205 ms | 149.8 MB |
| `a-plus-b` | 235 ms | 149.8 MB |
| `scanner+form` | **7.6 ms** | **2.5 MB** |
| `full` | **11.1 ms** | **2.4 MB** |

Only the form-stream cache moves this, and it moves it 18× in time and 60× in allocation.
Neither B nor A touches it — as expected, since excluding streams from the object cache is
exactly what leaves the re-read in place.

## Result 2 — ordinary documents

Paired sign test over the 69 corpus documents whose baseline exceeds 5 ms (below that, timer
resolution dominates), rotated order, 8 rounds. "faster"/"slower" count documents, `p` is a
two-sided binomial test:

| comparison | faster | slower | median ratio | p |
| --- | --- | --- | --- | --- |
| `scanner+form` vs `baseline` | 37 | 30 | 0.982 | 0.46 |
| `full` vs `baseline` | 34 | 33 | 0.999 | 1.00 |
| `full-v2` vs `baseline` | 35 | 32 | 0.991 | 0.81 |
| `full` vs `scanner+form` (adds cause A) | 29 | 38 | 1.007 | 0.33 |
| `full-v2` vs `scanner+form` (adds cause A) | 32 | 35 | 1.000 | 0.81 |
| `full-v2` vs `full` (the `StackDictionary` change) | 25 | 42 | 1.009 | 0.05 |

Reading this honestly:

- **No variant differs measurably from baseline on ordinary documents.** Every comparison against
  baseline is non-significant with a median ratio between 0.98 and 1.00. The pathology being
  fixed simply does not occur in these files, so there is nothing to win — and, importantly,
  nothing is lost either.
- **Cause A is free here.** Adding it costs a median 1.007 at p = 0.33 — indistinguishable from
  no effect. This supersedes the retracted p = 0.001 result, which was an ordering artifact.
- **The `StackDictionary` change bought nothing** (median 1.009, p = 0.05, in the *slower*
  direction if anything). It was proposed to remove a regression that turned out not to exist.

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
Unlike the timings, these figures are exact and stable across every sweep.

### The `StackDictionary` follow-up, and why it was dropped

The retracted result was blamed on `StackDictionary.TryGetValue`, one of the hottest paths in
the library: cause A changed its backing store from `List<Dictionary<K,V>>` to `List<Level>`,
where `Level` is a 16-byte struct, so each level probe copies a struct instead of loading a
reference. The theory was plausible, and the fix — levels back in a `List<Dictionary<K,V>>` with
the shared flags in a parallel `List<bool>` — was implemented and measured as `full-v2`.

It changed nothing (median 1.009, p = 0.05, marginally *slower*). The struct copy was never
costing anything measurable; the 2.4% it was meant to explain was the measurement order. **The
implementation change has been reverted**; `StackDictionary` keeps the `List<Level>` form from
`6921ae74`, which needs no parallel-list invariant.

What survived the exercise is worth keeping, and has been kept: `StackDictionaryTests` now covers
the copy-on-write invariant directly, which was previously untested. Those tests were verified to
fail when copy-on-write is removed, so they have teeth — and they pass against both
implementations, which is how the revert was checked.

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
corpus-wide allocation reduction. Worth taking even on its own.

**Cause B alone is not sufficient.** It leaves the reported document at ~15 seconds. If the bar
is "the document from #1390 opens in reasonable time", the scanner change does not clear it.

**Cause A — worth it, and it is free on ordinary documents.** It is solely responsible for the
last ~110×, taking the reported document from 15 s to 0.11 s. The measured cost elsewhere is
nil (median 1.007, p = 0.33). What it does cost is complexity: ~204 lines, an unbounded
per-document cache, and one non-obvious correctness invariant — `/Pattern` and `/Shading` must
stay uncached because they resolve through the resource stack. That invariant, not throughput, is
the real price, and it is worth paying for three orders of magnitude on a document shape that
real files exhibit.

**Form-stream cache — worth it on its own merits, not as part of #1390.** It contributes nothing
to the reported document and nothing measurable to the corpus. It fixes a genuine, separate
O(invocations × stream size) defect, worth 18× on a document with large repeatedly-invoked forms.
It also carries the only breaking API change in the whole fix. It would be entirely reasonable to
land it as a separate change, judged on its own.

**The `StackDictionary` follow-up was dropped.** It was recommended on the strength of a result
that did not survive scrutiny, and measurement confirmed it buys nothing, so the implementation
change was reverted. The new `StackDictionaryTests` were kept: they cover the copy-on-write
invariant that cause A depends on, which nothing tested before.

## What would change these conclusions

The corpus conclusions are "no detectable difference", not "no difference". With a per-document
round-to-round spread near 30% on this machine, a real effect below roughly 2–3% would not be
detected by 8 rounds. Anyone wanting a tighter bound should measure on a quiet machine with CPU
affinity pinned and many more rounds, keeping the rotated ordering.

The allocation figures are exact and need no such caveat; they are the more trustworthy half of
this document.
