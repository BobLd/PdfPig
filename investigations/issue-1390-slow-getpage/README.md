# Issue #1390 — `GetPage()` takes many minutes on certain documents

Investigation notes for [UglyToad/PdfPig#1390](https://github.com/UglyToad/PdfPig/issues/1390),
plus the reproduction documents and the harness used to measure them.

The cancellation-token request in the same issue is out of scope here.

For whether each piece of the fix earns its complexity — measured across five build variants and
the full 150-document test corpus — see [COST-BENEFIT.md](COST-BENEFIT.md).

**Status: both causes are fixed, and so is the form-stream re-read they left behind.** The
full-scale reproduction went from **3 min 35 s / 188 GB allocated** to **~290 ms / 82 MB**, and
the form-stream document from **466 ms / 150 MB** to **44 ms / 2.2 MB**. See
[Results](#results). Measurements in the two analysis sections below are the *original* ones,
kept as the record of the diagnosis.

## Summary

The reporter's document has a page with ~4 200 form-XObject invocations whose `/Resources`
all point at one shared dictionary containing a ~30 000-entry `/ExtGState`. There are **two
independent causes**, and they multiply:

- **Cause A** — `ResourceStore.LoadResourceDictionary` re-expands the whole resource
  dictionary on every form invocation. O(invocations x entries), no memoization.
- **Cause B** — `PdfTokenScanner` never caches objects read from a classic xref *table*, so
  every resolution of `/Resources 7 0 R` -> `/ExtGState 8 0 R` re-tokenizes that 30 000-entry
  dictionary from the raw file bytes.

A synthetic document of the reported shape takes **3 min 35 s** and allocates **188 GB** in
Release. Most of the wall clock is GC, not parsing (2 781 gen2 collections).

## Reproduction

```
Open:    150 ms
GetPage: 215065 ms, paths=4200, letters=0
         allocated 192584.2 MB, GC gen0/1/2: 4061/4047/2781
```

`documents/fullscale-extgstate-30k-4200do.pdf`, .NET 8, Release, no debugger attached. The
reporter measured ~300 ms per `LoadResourceDictionary` call against the ~51 ms measured here,
which is consistent with them stepping through under a debugger.

Cost is linear in both dimensions:

| document | `Do` count | GetPage |
| --- | --- | --- |
| `scaling-extgstate-30k-50do.pdf` | 50 | 2 945 ms |
| `scaling-extgstate-30k-100do.pdf` | 100 | 5 388 ms |
| `scaling-extgstate-30k-200do.pdf`\* | 200 | 10 375 ms |
| `fullscale-extgstate-30k-4200do.pdf` | 4 200 | 215 065 ms |

\* same file as `isolate-extgstate-indirect-30k-200do.pdf`.

## Cause A — `LoadResourceDictionary` re-expands the same dictionary every time

`BaseStreamProcessor.ProcessFormXObject` calls `ResourceStore.LoadResourceDictionary` on every
form invocation (`src/UglyToad.PdfPig/Graphics/BaseStreamProcessor.cs:557`), and
`ResourceStore.LoadResourceDictionary` (`src/UglyToad.PdfPig/Content/ResourceStore.cs:60`)
unconditionally walks every entry of `/Font`, `/XObject`, `/ExtGState`, `/ColorSpace`,
`/Pattern`, `/Properties` and `/Shading`, refilling a fresh `StackDictionary` level each time.

This is not specific to `/ExtGState` — every resource category has the same shape. Holding the
entry count at 30 000 and the `Do` count at 200, and varying only which key the entries sit
under:

| document | resource key | GetPage | allocated |
| --- | --- | --- | --- |
| `isolate-empty-resources-200do.pdf` | (empty `/Resources`) | 30 ms | 2 MB |
| `isolate-extgstate-1-entry-200do.pdf` | `/ExtGState`, 1 entry | 30 ms | 2 MB |
| `isolate-xobject-30k-200do.pdf` | `/XObject` | 3 288 ms | 1 988 MB |
| `isolate-colorspace-30k-200do.pdf` | `/ColorSpace` | 3 243 ms | 2 032 MB |
| `isolate-properties-30k-200do.pdf` | `/Properties` | 6 340 ms | 7 465 MB |
| `isolate-extgstate-direct-30k-200do.pdf` | `/ExtGState`, inline values | 8 433 ms | 8 243 MB |
| `isolate-extgstate-indirect-30k-200do.pdf` | `/ExtGState`, `n 0 R` values | 10 473 ms | 9 175 MB |

Secondary amplifiers inside the same method:

- `loadedNamedColorSpaceDetails` and `loadedColorSpaceDetailsCache` are cleared on **both**
  load and unload (`ResourceStore.cs:63-64` and `:203-204`), so the colour-space caches are
  discarded twice per form invocation.
- `PatternParser.Create` (`:158`) and `ShadingParser.Create` (`:184`) re-parse every pattern
  and shading from scratch on each load; these decode PDF functions and streams.
- `LoadFontDictionary` caches fonts reached by indirect reference but **not** direct font
  dictionaries — `loadedDirectFonts[...] = fontFactory.Get(fd)` at `:258` is unconditional, so
  an inline font dictionary has its font program re-parsed on every load.

## Cause B — the scanner never caches objects from a classic xref table

`PdfTokenScanner.Get` (`src/UglyToad.PdfPig/Tokenization/Scanner/PdfTokenScanner.cs:786-802`)
seeks, re-tokenizes and returns `found` **without calling `objectLocationProvider.Cache`**.
The only `Cache` calls in the file are at `:829` (brute-force scan) and `:863` (object
streams). So for any PDF that uses a classic `xref` table, every resolution of an indirect
reference re-reads and re-parses the object from the file.

Same logical document, differing only in how the objects are stored:

| document | storage | GetPage | allocated |
| --- | --- | --- | --- |
| `isolate-xobject-30k-200do.pdf` | classic xref table (no caching) | 3 288 ms | 1 988 MB |
| `objstm-xobject-30k-200do.pdf` | xref stream + object stream (caching works) | **590 ms** | **390 MB** |

5.6x faster purely from the caching path becoming reachable. The residual 590 ms / 390 MB
matches an isolated micro-benchmark of the 30 000-entry loop alone (2.8 ms and 1.9 MB per
call, x200), which confirms nothing else is hiding in the difference.

The same defect makes every `Do` re-read the form XObject's own stream, because
`ResourceStore.TryGetXObject` re-resolves the reference each time and `ProcessFormXObject`
then re-decodes and re-parses the content:

| document | form stream size | GetPage | allocated |
| --- | --- | --- | --- |
| `formstream-15b-200do.pdf` | 15 B | 30 ms | 2 MB |
| `formstream-196kb-200do.pdf` | 196 KB | 457 ms | 150 MB |

150 MB for 200 invocations of one 196 KB form ~ 750 KB re-materialised per `Do`.

This has been the behaviour since commit `2b486dcc` (June 2019) — it is not a regression.

## How the two causes interact

Because of cause B, each call to `LoadResourceDictionary` receives a **different
`DictionaryToken` instance** that is merely content-equal to the previous one. That is why the
reporter could only observe equal `GetHashCode()` values rather than reference identity.

A cheap `ReferenceEquals`-based "same resources as last time, skip" guard in `ResourceStore`
would therefore be a no-op today. Fixing B first makes object identity stable and makes that
guard viable. Otherwise a fix for A has to key on `DictionaryToken.GetHashCode`/`Equals`,
which are themselves O(n) and recurse into nested tokens
(`src/UglyToad.PdfPig.Tokens/DictionaryToken.cs:171-185`), partly defeating the purpose.

One caveat on fixing B: `ObjectLocationProvider`'s cache is unbounded, so caching every object
read from the file changes memory behaviour for large documents, and the interaction with
`ReplaceToken` / `overwrittenTokens` needs checking.

## Results

Causes A and B are fixed in commit `6921ae74`; the form-stream fix below it is in the working
tree, uncommitted.

**Cause B** — `PdfTokenScanner.Get` now calls `objectLocationProvider.Cache` on the classic
xref path, so an object listed directly in the xref table is tokenized once instead of on
every lookup. Streams are excluded: caching them would pin the raw bytes of every image and
content stream for the lifetime of the document, and object streams cannot contain streams, so
excluding them leaves both xref formats behaving the same.

**Cause A** — `ResourceStore` resolves a resource dictionary once and caches the resulting
levels against it by reference identity (`ResolvedResources`, keyed with
`ReferenceEqualityComparer<DictionaryToken>`). `StackDictionary` gained a `Push(level)`
overload that shares a pre-computed level and copies it before any write. `/Pattern` and
`/Shading` are deliberately **not** cached: they resolve colour spaces through the resource
stack via `resourceStore.GetColorSpaceDetails`, so their result depends on the levels below
and is not a function of the resource dictionary alone.

**Form-stream re-read** — with streams excluded from the object cache, a form XObject was still
resolved, decoded and re-parsed on every `Do`. `BaseStreamProcessor` now keeps two per-page
caches: the form's `StreamToken` keyed by its `IndirectReference` (populated in `ApplyXObject`
only when the subtype is `/Form`), and its parsed operations keyed by that stream's identity.
Both die with the page, and images — the large streams — are never cached, so the memory
argument for excluding streams from the object cache still holds. `ApplyXObject` finds the
reference through a new `IResourceStore.TryGetXObjectReference`, which reads the current
XObject scope without resolving the object it points at.

This ordering matters — cause A's fix depends on cause B's. Before B, every load received a
different `DictionaryToken` instance, so an identity-keyed cache would never hit.

| document | before | after B | after A + B | after form fix |
| --- | --- | --- | --- | --- |
| `fullscale-extgstate-30k-4200do.pdf` | 215 065 ms / 188 GB | 13 284 ms / 7.9 GB | 286 ms / 97 MB | **291 ms / 82 MB** |
| `isolate-extgstate-indirect-30k-200do.pdf` | 10 473 ms / 9 175 MB | 999 ms / 427 MB | 204 ms / 55 MB | **242 ms / 54 MB** |
| `isolate-properties-30k-200do.pdf` | 6 340 ms / 7 465 MB | — | 128 ms / 44 MB | **146 ms / 43 MB** |
| `isolate-xobject-30k-200do.pdf` | 3 288 ms / 1 988 MB | 688 ms / 389 MB | 84 ms / 17 MB | **94 ms / 16 MB** |
| `isolate-colorspace-30k-200do.pdf` | 3 243 ms / 2 032 MB | — | 72 ms / 17 MB | **82 ms / 16 MB** |
| `objstm-xobject-30k-200do.pdf` | 590 ms / 390 MB | — | 84 ms / 18 MB | **95 ms / 17 MB** |
| `formstream-196kb-200do.pdf` | 457 ms / 150 MB | 466 ms / 150 MB | 464 ms / 150 MB | **45 ms / 2.2 MB** |
| `formstream-15b-200do.pdf` | 30 ms / 2 MB | — | — | **35 ms / 1.4 MB** |

The "after A + B" and "after form fix" columns were measured in different sessions, so small
time differences between them are machine noise; the allocation figures are exact and directly
comparable. A back-to-back A/B of the form fix alone, on one machine state, gave
466/557 ms → 45/43 ms for `formstream-196kb-200do.pdf` and 343/330/372 ms → 315/270/334 ms for
`fullscale-extgstate-30k-4200do.pdf`.

Gen2 collections on the full-scale document went from 2 781 to 0.

### What is not fixed

Three pre-existing issues were left alone because fixing them is not needed for #1390 and would
widen the change:

- `HasFormXObjectCircularReference` still runs `operations.OfType<InvokeNamedXObject>().Any(...)`
  over the cached operation list on every invocation. It is a cheap type test per operation and
  the expensive remainder only runs for a genuinely self-referencing form, so it was left as is.

- `loadedDirectFonts` is keyed by name in a store shared across resource dictionaries, so two
  resource dictionaries that both define an inline `/F1` collide. The caching preserves the
  existing behaviour by re-applying each inline font's name binding on every load; only the
  parse is skipped.
- `loadedNamedColorSpaceDetails` and `loadedColorSpaceDetailsCache` are still cleared on both
  load and unload.

### Tests

- `PdfTokenScannerTests.GetResolvesObjectFromXrefTableOnlyOnce` — an object listed in the xref
  table resolves to the same instance on repeated lookups.
- `PdfTokenScannerTests.GetDoesNotCacheStreamObjects` — records the deliberate memory trade-off.
- `ResourceStoreCachingTests.ReloadingTheSameResourceDictionaryResolvesNoFurtherObjects` —
  reloading resolves nothing through the scanner (it went 3 objects → 6 before the fix).
- `ResourceStoreCachingTests.ReloadingTheSameResourceDictionaryStillResolvesItsEntries` — guard
  that a memoized reload returns the same values as the first load.
- `FormXObjectCachingTests.RepeatedFormInvocationResolvesTheStreamOnce` — a second `Do` on the
  same form resolves nothing further through the scanner (it went 1 → 2 before the fix).
- `FormXObjectCachingTests.RepeatedFormInvocationParsesTheContentStreamOnce` — the form's content
  stream reaches `IPageContentParser` once for two invocations (1 → 2 before the fix).
- `FormXObjectCachingTests.RepeatedFormInvocationRunsTheContentEveryTime` — guard that caching
  the parse does not cache the *execution*: three invocations still produce three paths.

Full suite: 0 failed on every test target framework — 4 208 passed / 7 skipped on net8.0,
4 209 on net9.0, 4 206 on net471. All seven library target frameworks build clean.

## Unrelated observation

`NameToken.Create` interns every name into a static, process-wide `ConcurrentDictionary` that
is never trimmed (`src/UglyToad.PdfPig.Tokens/NameToken.cs:32` and
`NameToken.Constants.cs:7`). This document permanently adds 30 000 entries to it, which
matters for long-running services that open untrusted PDFs.

## Running it

```sh
# from this folder
dotnet run -c Release --project repro/Repro.csproj -- documents/isolate-xobject-30k-200do.pdf
```

The harness prints elapsed time for `GetPage(1)` plus allocated bytes and GC counts.

The documents are generated, not captured from a real file — the reporter's PDF is
user-provided and could not be shared. To regenerate them:

```sh
python repro/make-classic-xref-pdf.py 30000 4200 documents/fullscale-extgstate-30k-4200do.pdf extg-indirect
python repro/make-classic-xref-pdf.py 30000   50 documents/scaling-extgstate-30k-50do.pdf      extg-indirect
python repro/make-classic-xref-pdf.py 30000  100 documents/scaling-extgstate-30k-100do.pdf     extg-indirect
python repro/make-classic-xref-pdf.py 30000  200 documents/isolate-extgstate-indirect-30k-200do.pdf extg-indirect
python repro/make-classic-xref-pdf.py 30000  200 documents/isolate-extgstate-direct-30k-200do.pdf   extg-direct
python repro/make-classic-xref-pdf.py 30000  200 documents/isolate-xobject-30k-200do.pdf     xobject
python repro/make-classic-xref-pdf.py 30000  200 documents/isolate-properties-30k-200do.pdf  properties
python repro/make-classic-xref-pdf.py 30000  200 documents/isolate-colorspace-30k-200do.pdf  colorspace
python repro/make-classic-xref-pdf.py     1  200 documents/isolate-extgstate-1-entry-200do.pdf extg-indirect
python repro/make-classic-xref-pdf.py     1  200 documents/isolate-empty-resources-200do.pdf   none
python repro/make-objstm-pdf.py       30000  200 documents/objstm-xobject-30k-200do.pdf
python repro/make-large-form-pdf.py     200    0 documents/formstream-15b-200do.pdf
python repro/make-large-form-pdf.py     200 4000 documents/formstream-196kb-200do.pdf
```

Measurements above were taken on .NET 8, Release, Windows 11.
