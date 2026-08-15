# Rendering intent is applied when a colour is *set*, not when it is *painted*

**Branch:** `feature/icc-profile-support-5`
**Date raised:** 2026-08-13
**Status:** ✅ **Fixed** — Option A implemented, 2026-08-13. See §8 for what was built and §11 for how it differs from the plan.
**Related:** `icc-output-intent-review.md` (rendering intent threading, finding A2/A3)

> **Resolution.** `CurrentGraphicsState` now keeps each current colour as a `DeferredColor` — the colour space plus the operands it was selected with, plus the colour already converted under the intent in force at selection. Reading `CurrentStrokingColor` / `CurrentNonStrokingColor` reconverts only if the graphics state's intent has moved since, and stores the result back so a page painted under a changed intent converts once rather than once per letter. Sections 1–7 below describe the problem as found and are left unchanged; §8 records what was implemented.

---

## 1. The problem in one sequence

PdfPig resolves a colour to a concrete `IColor` at the moment the colour-setting operator runs, and applies the rendering intent that is in force *at that moment*. The rendering intent is a graphics state parameter that can change afterwards, before anything is painted:

```
/CS0 cs              % ICCBased colour space; initial colour resolved NOW,
                     % using the intent currently in the graphics state
0.1 0.2 0.3 scn      % colour resolved NOW, same intent
/Perceptual ri       % intent changes
0 0 100 100 re f     % painted HERE, with a colour computed under the OLD intent
```

The fill uses a colour converted through the profile's **RelativeColorimetric** transform even though the graphics state says **Perceptual** by the time the `f` operator executes. Nothing detects or corrects this.

The same happens when the intent arrives through an ExtGState rather than the `ri` operator (`BaseStreamProcessor.cs:907`), which is the more common route in real files:

```
/CS0 cs
0.1 0.2 0.3 scn
/GS0 gs              % ExtGState with /RI /Perceptual
0 0 100 100 re f     % same staleness
```

## 2. Why this is wrong

ISO 32000-1 8.6.5.8 makes the rendering intent a *graphics state* parameter (Table 58), and it governs colour **rendering** — the conversion from the source colour space to the output device. That conversion conceptually belongs to the painting operator, not to `cs`/`scn`. The intent that should apply is the one in force when the mark is made.

PdfPig instead binds the intent at colour-set time. For every colour space whose conversion ignores the intent this is unobservable; for an ICCBased space backed by a real profile it is a wrong colour, because `ICCBasedColorSpaceDetails.GetTransformWithFallback(intent)` resolves a **different transform per intent**.

## 3. Scope — this is not specific to `GetInitializeColor`

The staleness was noticed while adding the `RenderingIntent` parameter to `GetInitializeColor`, but that method is only one of five eager resolution points. All of them live in `ColorSpaceContext` and all bind the intent identically:

| Operator | Site | Call |
|---|---|---|
| `CS` | `ColorSpaceContext.cs:35` | `GetInitializeColor(state.RenderingIntent)` |
| `SC` / `SCN` | `ColorSpaceContext.cs:53` | `GetColor(operands, state.RenderingIntent)` |
| `cs` | `ColorSpaceContext.cs:84` | `GetInitializeColor(state.RenderingIntent)` |
| `sc` / `scn` | `ColorSpaceContext.cs:102` | `GetColor(operands, state.RenderingIntent)` |
| `g`/`rg`/`k` and stroking variants | `ColorSpaceContext.cs:132` | `GetColor(values, state.RenderingIntent)` |

Each writes a fully resolved `IColor` into `CurrentGraphicsState.CurrentStrokingColor` / `CurrentNonStrokingColor` (`CurrentGraphicsState.cs:101`, `:106`), both typed `IColor` — component values and the owning colour space are discarded at that point and cannot be reconverted later.

Adding the intent parameter to `GetInitializeColor` made that method *consistent with the other four*. It did not make any of the five correct with respect to ordering.

## 4. Where the resolved colour is consumed

The consumption points are all at painting time, which is exactly where a deferred resolution would want to happen:

| Consumer | Site | When |
|---|---|---|
| Path stroke colour | `PdfPath.cs:99` (`SetStrokeDetails`) | called from `ContentStreamProcessor.cs:423`, inside the path-painting operator |
| Path fill colour | `PdfPath.cs:112` (`SetFillDetails`) | called from `ContentStreamProcessor.cs:428`, same |
| Letter colours | `ContentStreamProcessor.cs:200-201` | at glyph show time |

This is the useful part of the finding: **the consumers already run at the right moment.** The problem is purely that the value they read was computed too early.

## 5. How PDFBox avoids it

PDFBox never resolves eagerly. `PDColorSpace.getInitialColor()` and the colour operators produce a `PDColor`, which holds `float[] components` plus the owning `PDColorSpace` and nothing else — the class comment is explicit that "color values are not associated with any given color space" for conversion purposes. For ICCBased the initial colour is precomputed once in the constructor as pure component data (`PDICCBased.java:206`).

Conversion happens at paint time, in the renderer:

```java
// PageDrawer.java:689
getPaint(graphicsState.getStrokingColor()), graphicsState.getSoftMask());
// PageDrawer.java:704
getPaint(graphicsState.getNonStrokingColor()), graphicsState.getSoftMask());
```

**Important caveat: PDFBox's immunity here is vacuous.** `PDColorSpace.toRGB(float[])` takes no rendering intent, and `PDGraphicsState.renderingIntent` is parsed and then never read by any conversion. PDFBox would be correct *if* it applied intent — it simply doesn't have the problem because it doesn't have the feature. PdfPig threading `RenderingIntent` end-to-end is a deliberate divergence (see `icc-output-intent-review.md` §1), and this ordering issue is a consequence of that divergence, not a porting defect.

So PDFBox supplies the **architecture** worth copying (defer conversion) but not a worked answer.

## 6. How much does it matter?

Low frequency, non-zero impact:

- It requires `ri` or a `/RI`-bearing `gs` to appear **after** a colour-setting operator and **before** a painting operator, within the same `q`/`Q` level or an enclosing one. Most producers emit `gs` early in a content stream, before colour selection.
- It is only observable when `ParsingOptions.IccProfileService` is configured **and** the profile actually returns distinct transforms per intent. With no service configured (the default) PdfPig is entirely unaffected, because every non-ICC conversion ignores the intent.
- When it does bite, the error is a subtly wrong colour, not a crash or a failed parse.

That combination is why this is documented rather than fixed on this branch.

## 7. Options

### Option A — Defer resolution behind the existing property (recommended)

Keep `CurrentGraphicsState.CurrentStrokingColor` / `CurrentNonStrokingColor` typed `IColor`, but back them with the unresolved operands and resolve on read, using the intent current at that read.

```csharp
private ColorSpaceDetails? strokingSpace;
private double[]? strokingOperands;
private IColor? strokingResolved;
private RenderingIntent strokingResolvedUnder;

public IColor CurrentStrokingColor
{
    get
    {
        if (strokingResolved is null || strokingResolvedUnder != RenderingIntent)
        {
            strokingResolved = strokingSpace!.GetColor(strokingOperands, RenderingIntent);
            strokingResolvedUnder = RenderingIntent;
        }

        return strokingResolved;
    }
    set { /* pre-resolved colour set directly; see risks */ }
}
```

**Why this shape:** the public type does not change, so `Letter`, `PdfPath`, `IColor` and every downstream renderer are untouched. Resolution moves to read time, and every read site is already a painting site (§4). The memoisation keeps the cost at one conversion per (operands, intent) pair rather than one per letter.

**Cost:** two reference fields, one `double[]`, and one enum per colour per graphics state — no collections, no cache keyed on anything global.

### Option B — Follow PDFBox literally

Replace `IColor CurrentStrokingColor` with a `PdfColor`-style value holding operands + colour space, and convert at each consumer.

Rejected for now: `CurrentGraphicsState` is public (`CurrentGraphicsState.cs:15`), both properties are public and settable, and the resolved colours are re-exposed publicly as `Letter.Color` / `Letter.StrokeColor` / `Letter.FillColor` (`Letter.cs:98`, `:103`, `:108`) and `PdfPath.StrokeColor` / `PdfPath.FillColor`. This is a breaking change across the whole public colour surface for a low-frequency correctness gain.

### Option C — Leave as is, document

What this file does. Reasonable while ICC support is opt-in and unreleased.

## 8. Option A as implemented

`Graphics/DeferredColor.cs` (new, internal `readonly struct`) holds one of two things:

- **Fixed** — a colour handed over ready-made, with no colour space behind it. Stands exactly as given, forever. This is what the public setters store and what a Pattern colour is.
- **From operands** — a colour space, the operands (or `null`, meaning "the space's own initial colour"), the converted colour, and the intent it was converted under.

`Resolved(RenderingIntent)` returns itself when the intent is unchanged or there is nothing to reconvert from, and otherwise returns a new struct converted under the new intent. `CurrentGraphicsState`'s two property getters call it and **store the result back**, so the reconversion happens once per intent change rather than once per read.

What each planned step became:

1. **Failing tests first** — five added to `InitializeColorRenderingIntentTests`, covering `cs`-then-`ri`, `scn`-then-`ri` (both stroking and non-stroking), the `rg`-with-`/DefaultRGB`-ICCBased route, and `DeepClone`. All five failed before the change and pass after. A sixth, `ADirectlyAssignedColour_IsNeverReDerived`, passed both before and after — it is the guard against over-fixing.
2. **Operands into `CurrentGraphicsState`** — done, as two `DeferredColor` fields plus internal `SetStrokingColor` / `SetNonStrokingColor` methods taking `(ColorSpaceDetails, double[]?, RenderingIntent)`.
3. **Repoint `ColorSpaceContext`** — done for all five sites. **The planned new `ColorSpaceDetails` API was not needed**: see §11.
4. **Pattern and Unsupported** — Pattern still goes through the public setter and so becomes a `Fixed` colour with nothing to reconvert; the early return for `UnsupportedColorSpaceDetails` is untouched.
5. **`DeepClone`** — copies the two `DeferredColor` fields directly rather than going through the properties, with a comment saying why. The `double[]` is aliased, not copied, which is safe because the operand arrays are never mutated after being handed over; `DeferredColor`'s XML docs state that requirement.
6. **Public setter** — **not** preserved. Both setters are now `[Obsolete]` and throw `NotSupportedException`; see §8a.
7. **Visual verification** — run as part of the full suite: 4298 passed, 0 failed, 7 skipped.

## 8a. The colour setters are deprecated and throw

`CurrentStrokingColor` and `CurrentNonStrokingColor` keep their getters. Their **setters** are `[Obsolete]` and throw `NotSupportedException`.

The reason is that the plan's step 6 — accept an assigned colour as a `Fixed` one — quietly reintroduces the bug this file is about. A colour handed straight to the graphics state carries no colour space and no operands, so it can never answer a later `ri`; it silently pins itself to whatever intent happened to be in force. Accepting it means the property sometimes honours the intent and sometimes does not, with nothing at the call site to say which. Refusing is the honest behaviour: the setter cannot deliver what the getter now promises.

Colours are selected through the operators on `IColorSpaceContext`, which supply both a colour space and operands. Internally, `CurrentGraphicsState` has exactly one entry point per colour:

```csharp
internal void SetStrokingColor(DeferredColor color) => stroking = color;
internal void SetNonStrokingColor(DeferredColor color) => nonStroking = color;
```

The two ways a colour can arise are the two `DeferredColor` factories rather than overloads on the graphics state, so there is one way in and the choice is made where it is meaningful:

- `DeferredColor.FromOperands(space, operands, intent)` — reconvertible, the normal case. `null` operands mean the space's own initial colour.
- `DeferredColor.Fixed(color)` — for the two cases that genuinely have nothing to reconvert from: a **Pattern** colour, which is selected by name rather than by component values and so has no operands at all, and a **plain device** colour space, which cannot vary by intent and is kept allocation-free for the reasons in §9.

**Consequence to be aware of:** this removes the only public way to set a current colour. External code that assigned these properties directly has no replacement, because `SetStrokingColor` and `DeferredColor` are both `internal`. If a public equivalent turns out to be wanted, expose something that takes a colour space and operands — not one that takes a bare `IColor`, which would put the original bug back in reach.

## 9. Keeping the default path free of cost

With no `IIccProfileService` configured, no colour space varies by intent, so the reconversion can never change an answer. Two things keep it from costing anything anyway:

- **`g`/`rg`/`k` retain nothing.** `SetDeviceColor` checks whether the resolved space is one of the three device singletons and, if so, converts eagerly and stores a `Fixed` colour exactly as before — the operand span never leaves the stack and no `double[]` is allocated. These are the hottest colour operators in any content stream. Operands are retained only when 8.6.5.6 has remapped the operator to a `/DefaultGray`, `/DefaultRGB` or `/DefaultCMYK` substitute, which is the one route by which a device operator becomes intent-dependent.
- **`sc`/`scn` allocate nothing new.** That path already receives a `double[]` from the operator; it is now stored rather than discarded.

In the ordinary ordering — intent set before the colour, or never set at all — `Resolved` takes its `currentIntent == intent` fast path and no conversion runs beyond the eager one that always ran.

## 10. Risks, and how each landed

- **Per-read conversion cost.** Addressed by storing the resolved struct back in the property getter, so an intent change costs one conversion, not one per letter. The invalidation condition is a single equality check against the intent the colour was last converted under; a new selection replaces the whole struct, so there is no stale-memo path.
- **Mutable state in the graphics state.** Contained in one `readonly struct` per colour rather than loose fields, and `DeepClone` copies both structs directly. This was the step most likely to be got wrong and has its own test (`DeepClone_CarriesTheOperandsSoTheCloneStillFollowsItsOwnIntent`), which asserts both that the clone follows its own intent and that the original is unaffected.
- **Behavioural change in existing output.** No visual-verification baseline moved: the full suite passes unchanged. Expected, since a baseline document would need both an ICC service configured and an intent change between colour and paint.
- **Null operands.** `Fixed` is the explicit representation of "nothing to reconvert from", covering Pattern colours and externally-assigned ones, and is what the `colorSpace is null` check in `Resolved` keys off.

## 11. Where the implementation departs from the plan

**No new `ColorSpaceDetails` API was needed.** The plan (step 3) assumed `GetInitializeColor` would need a companion yielding initial *operands*, since the initial colour is derived by each space rather than selected by the operator. That turned out to be unnecessary: `null` operands are themselves the discriminator. `DeferredColor` calls `GetInitializeColor(intent)` when the operand array is `null` and `GetColor(operands, intent)` when it is not, so the initial colour is re-derived under the new intent by the same method that derived it originally. No public surface changed at all — this fix is entirely internal.

## 12. What was already correct and left alone

- Threading `RenderingIntent` through `GetColor`, `GetRgb`, `Process`, `Transform`, `GetInitializeColor` and `ColorSpaceDetailsByteConverter.Convert`. The parameters were right; only the *timing* of the call was wrong, and only the timing changed.
- Image conversion. Images carry their own `RenderingIntent` on `IPdfImage` (`IPdfImage.cs:52`) and convert when the image is decoded, not when a colour space was set — the image path never had this problem.
