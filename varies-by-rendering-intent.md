# `ColorSpaceDetails.VariesByRenderingIntent` — what the work involves

**Status:** ✅ **Implemented.** Sections 1–9 are the plan as written beforehand and are left unchanged;
§10 records what was built, §11 where it departs from the plan, §12 how it was verified.
**Related:** `rendering-intent-eager-colour-resolution.md` (the `DeferredColor` design this builds on).

---

## 1. What it is

A `bool` on `ColorSpaceDetails` answering: *can this colour space produce a different colour for the same
operands under a different rendering intent?*

It exists because `DeferredColor` retains the operands of every selected colour so that a later `ri` (or an
ExtGState `/RI`) can reconvert it. That retention is only ever useful for a colour space that actually reads
the intent. For every other space the retained array is dead weight — and on the `g`/`rg`/`k` path it is a
heap allocation per operator.

`ColorSpaceContext.SetDeviceColor` already makes this decision, but with a hardcoded special case:

```csharp
bool isPlainDevice = colorSpace is DeviceGrayColorSpaceDetails
    or DeviceRgbColorSpaceDetails
    or DeviceCmykColorSpaceDetails;
```

That covers three of the twelve implementations. The proposal is to replace it with the general question,
answered by each class about itself.

## 2. The evidence it rests on

Every `ColorSpaceDetails` implementation was checked for whether its conversion actually *consumes* the
`RenderingIntent` parameter, as opposed to merely declaring and forwarding it:

| Class | Varies by intent? | Why |
|---|---|---|
| `ICCBasedColorSpaceDetails` | **Yes**, iff `IccProfile is not null` | `GetTransformWithFallback(intent)` resolves a different `IIccTransform` per intent. With no profile it returns `null` immediately and the call delegates to `AlternateColorSpace`. |
| `IndexedColorSpaceDetails` | Transitively | `GetColor` looks the index up in the palette, then converts through `BaseColorSpace` (`:134`). |
| `SeparationColorSpaceDetails` | Transitively | Evaluates the tint function, then `AlternateColorSpace.Process(evaled, intent)` (`:75`). |
| `DeviceNColorSpaceDetails` | Transitively | Same shape (`:90`). |
| `PatternColorSpaceDetails` | Transitively | Only through `UnderlyingColourSpace`, for an uncoloured tiling pattern. |
| `LabColorSpaceDetails` | **No** | Takes the parameter, forwards it internally to its own `GetRgb`, never reads it. |
| `CalRGBColorSpaceDetails` | **No** | Same. |
| `CalGrayColorSpaceDetails` | **No** | Same. |
| `DeviceGray` / `DeviceRgb` / `DeviceCmyk` | **No** | Fixed formulae. |
| `UnsupportedColorSpaceDetails` | **No** | Converts nothing. |

So exactly **one** leaf class is intent-dependent, and only in one of its two states. The three `Cal*`/`Lab`
spaces are the interesting finding: they look intent-aware from their signatures and are not, so today they
retain operands for nothing.

## 3. The change

### 3.1 API addition

```csharp
// ColorSpaceDetails
/// <summary>
/// Whether this colour space can convert the same operands to a different colour under a different
/// rendering intent. <see langword="false"/> lets a caller that would otherwise retain the operands
/// (to answer a later ri) convert once and keep only the result.
/// Over-reporting is safe; under-reporting silently ignores a later intent change.
/// </summary>
public virtual bool VariesByRenderingIntent => false;
```

`ColorSpaceDetails` is public and unsealed-by-design (`protected internal` ctor), so this is **public API
surface** — a virtual property on a public abstract class. Defaulting to `false` keeps any third-party
subclass compiling, at the cost of defaulting them to the unsafe answer if they *are* intent-dependent.
Defaulting to `true` would be the conservative choice and costs nothing but the optimisation; worth a
deliberate decision. (Recommendation: `false`, matching every in-tree leaf, and call it out in the doc
comment — as written above.)

### 3.2 Per-class overrides

```csharp
// ICCBasedColorSpaceDetails
public override bool VariesByRenderingIntent => IccProfile is not null;

// IndexedColorSpaceDetails
public override bool VariesByRenderingIntent => BaseColorSpace.VariesByRenderingIntent;

// SeparationColorSpaceDetails, DeviceNColorSpaceDetails
public override bool VariesByRenderingIntent => AlternateColorSpace.VariesByRenderingIntent;

// PatternColorSpaceDetails
public override bool VariesByRenderingIntent => UnderlyingColourSpace?.VariesByRenderingIntent ?? false;
```

Nothing else overrides. Five overrides, one virtual.

Note on the ICCBased answer: a profile that only supports `RelativeColorimetric` makes every intent fall
back to the same transform, so it does not *really* vary — but finding that out means probing
`TryGetTransform` for each intent. `IccProfile is not null` over-reports in that case, which is the safe
direction.

The composite properties are computed, not cached. They walk one level per call and the chain is short
(`Indexed → Separation → ICCBased` is about as deep as it gets in practice), but if it ever shows up they
can be resolved once in each constructor — the child space is immutable and assigned there.

### 3.3 Call site

`ColorSpaceContext.SetDeviceColor` — replace the three-way type test with the property:

```csharp
- bool isPlainDevice = colorSpace is DeviceGrayColorSpaceDetails
-     or DeviceRgbColorSpaceDetails
-     or DeviceCmykColorSpaceDetails;
+ // Nothing to reconvert from unless the space actually reads the intent, so most colours keep only
+ // the converted result and their operands never leave the stack.
+ bool isFixed = !colorSpace.VariesByRenderingIntent;
```

That is the whole functional change. The existing comment above the check already describes this rule; it
becomes accurate for every space rather than for three.

## 4. What it buys

Operand arrays allocated per device colour operator (`g`/`rg`/`k`), net8.0+:

| `SetDeviceColor` reaches | today | with the property |
|---|---|---|
| a plain device space (no `/Default*` substitution) | 0 | 0 |
| `/DefaultGray\|RGB\|CMYK` → `Lab`, `CalRGB`, `CalGray` | **1** | **0** |
| `/Default*` → ICCBased with no usable profile | **1** | **0** |
| `/Default*` → ICCBased with a live profile | 1 | 1 |

Measured cost of one such array on the real path (Release, net9.0, 100k calls through
`ColorSpaceContext.SetNonStrokingColorRgb`/`Gray`): **48 B/call for `rg`, 32 B/call for `g`** — roughly
double the total allocation of the operator, since the only other allocation is the `RGBColor`/`GrayColor`
itself.

So the win is confined to documents that define a `/DefaultRGB`-family entry pointing at a non-ICC or
unusable-ICC space. That is not the common case. **This is a tidiness-and-correctness change with an
allocation win attached, not primarily a performance change** — the honest framing, given
`SetDeviceColor` already handles the hot case.

Secondary benefits, both real but small:

- **Deletes the hardcoded triple type-test**, which silently misses any future device-like space and has to
  be remembered by whoever adds one.
- **Makes the `Cal*`/`Lab` finding executable** rather than a comment: today those spaces retain operands
  and force a `DeferredColor.Resolved` comparison on every colour read, for a conversion that cannot move.

## 5. Risks

**The failure mode is one-directional and silent.** A space that answers `false` while actually varying will
have its colour pinned to the intent in force at selection, and a later `ri` will be ignored — no exception,
no wrong-looking output unless someone compares against a reference. The dangerous case is a composite that
forgets to forward: e.g. `SeparationColorSpaceDetails` inheriting the default `false` while its alternate is
an ICC-backed space.

Mitigation is to test the forwarding specifically, not just the leaves — see §6.

**Not a risk:** answering `true` when the space does not vary. That only forgoes the optimisation.

## 6. Testing

Existing coverage that already exercises the `ri`-after-colour path and must stay green:

- `InitializeColorRenderingIntentTests` — `cs`-then-`ri` and `scn`-then-`ri`, both stroking and
  non-stroking, plus the `rg`-with-`/DefaultRGB`-ICCBased route and a `DeepClone` case. The
  `/DefaultRGB`-ICCBased test is the one that pins the branch this change touches.
- `UncolouredTilingPatternColorTests.IntentSetAfterTheOperatorStillAppliesToTheUnderlyingColour` — the
  pattern equivalent.

To add:

1. **One test per composite**, asserting the property forwards: an `Indexed` over an ICC-backed base, a
   `Separation` and a `DeviceN` over an ICC-backed alternate, and a `Pattern` over one, each asserting
   `VariesByRenderingIntent` is `true`; and the same four over a device base/alternate asserting `false`.
   These are cheap — they need no conversion, just construction.
2. **One behavioural test per composite** that actually changes the intent after selecting a colour and
   asserts the colour moves. This is what catches a forwarding bug; the property assertions above would pass
   against a wrong-but-consistent implementation.
3. **A negative guard** that a `Lab`/`CalRGB`/`CalGray` colour does *not* move when the intent changes —
   pinning the §2 finding so a future change to those classes has to confront it.

**Verification method that has worked on this branch:** implement, then neuter the mechanism (make
`VariesByRenderingIntent` return a constant) and confirm exactly the intended tests fail. A constant `true`
should fail nothing (it only disables the optimisation) — that is itself a useful assertion that the change
is behaviour-preserving. A constant `false` should fail every behavioural test in (2) and the existing
`/DefaultRGB`-ICCBased case.

## 7. Considered and rejected

**Putting the check inside `DeferredColor.FromOperands` instead of at the call site.** Tempting, because it
would cover every caller at once (`sc`/`scn` and the `cs`/`CS` initial colour, not just `g`/`rg`/`k`). But
`FromOperands` takes `double[]`, so the array would already have been materialised by the time the check
ran — the allocation the change exists to avoid. The decision has to happen where the operands are still a
span, which is `SetDeviceColor`. The other call sites gain nothing anyway: `sc`/`scn` receives an array the
operation object already owns, so retaining it is free.

**Caching the composite answers in the constructors.** Correct and cheap, but premature: the property is
read once per colour-setting operator, and the walk is one virtual call per level over an immutable chain.
Left as a note in §3.2.

## 8. Adjacent, not part of this

`IndexedColorSpaceDetails`, `SeparationColorSpaceDetails` and `DeviceNColorSpaceDetails` each key a colour
cache by `(value, intent)`:

```csharp
private readonly ConcurrentDictionary<(double Index, RenderingIntent Intent), IColor> cache = new();
private readonly ConcurrentDictionary<(double Tint, RenderingIntent Intent), IColor> cache = new();
// DeviceN: TintKey(values, intent)
```

When the space does not vary by intent, the intent component of that key is redundant, and a page that
switches intent stores every colour twice. Dropping it conditionally would halve those caches in the worst
case. It is a separate change with its own risk (cache keys are load-bearing for correctness — see finding
#4 in the ICC review, where adding the intent to these keys was itself the fix) and should not be bundled
in.

## 9. Size

| | |
|---|---|
| Public API added | 1 virtual property |
| Overrides | 5 |
| Behavioural call sites changed | 1 (`SetDeviceColor`) |
| Lines deleted | 3 (the type test) |
| Tests to add | ~12 |

---

## 10. What was built

Exactly the shape of §3, at the estimated size: one virtual, five overrides, one call site.

| File | Change |
|---|---|
| `Graphics/Colors/ColorSpaces/ColorSpaceDetails.cs` | `public virtual bool VariesByRenderingIntent => false;` |
| `…/ICCBasedColorSpaceDetails.cs` | `=> IccProfile is not null \|\| AlternateColorSpace.VariesByRenderingIntent` |
| `…/IndexedColorSpaceDetails.cs` | `=> BaseColorSpace.VariesByRenderingIntent` |
| `…/SeparationColorSpaceDetails.cs` | `=> AlternateColorSpace.VariesByRenderingIntent` |
| `…/DeviceNColorSpaceDetails.cs` | `=> AlternateColorSpace.VariesByRenderingIntent` |
| `…/PatternColorSpaceDetails.cs` | `=> UnderlyingColourSpace?.VariesByRenderingIntent ?? false` |
| `Graphics/ColorSpaceContext.cs` | `isPlainDevice` type test → `bool isFixed = !colorSpace.VariesByRenderingIntent;` |
| `…Tests/Graphics/Colors/VariesByRenderingIntentTests.cs` | new, 14 tests |

**The §3.1 open question was decided as recommended: the virtual defaults to `false`.** Every leaf in the
tree answers `false`, so the default is the common case, and the doc comment states plainly that
over-reporting is harmless while under-reporting is silent — a third-party subclass converting through
another space has to forward it. Defaulting to `true` would have been safe-by-construction but would have
left every in-tree class needing an override to say the ordinary thing.

## 11. Where the implementation departs from the plan

**`ICCBasedColorSpaceDetails` also consults its alternate.** The plan had it answer `IccProfile is not null`
alone. That is not a complete account of the class:

- with no profile, `GetTransformWithFallback` returns `null` and *every* conversion delegates to
  `AlternateColorSpace`;
- even with a profile, `GetColor`/`GetRgb`/`Process` fall through to the alternate when `TryToRgb` fails
  (a malformed profile is not held against the space — the next colour tries it again).

So an ICCBased space with an unusable profile over, say, a `Separation` whose own alternate is ICC-backed
would have reported `false` while genuinely varying — the one direction §5 calls unsafe. The override is
therefore `IccProfile is not null || AlternateColorSpace.VariesByRenderingIntent`.

The `Lab`-profile over-reporting noted in §3.2 was left as planned: a profile supporting only
`RelativeColorimetric` still answers `true`, because establishing otherwise means probing the profile per
intent, and over-reporting costs nothing but the optimisation.

**One planned test was dropped, deliberately.** §6(3) called for a behavioural guard that a
`Lab`/`CalRGB`/`CalGray` colour does not move when the intent changes. It would have asserted "the colour
stays the same" — which was equally true before this change, since reconversion through those spaces is a
no-op. It therefore proves nothing about the property. `TheCieBasedSpacesDoNotVary` asserts the property
directly instead, which is the honest form of the same guard and is what a future change to those classes
would trip over.

**14 tests rather than the estimated ~12**, from writing the composite forwarding as `[Theory]` over both
branches rather than as pairs of facts.

## 12. Verification

Beyond the new tests passing, the plan's neuter-and-check method (§6) was applied in both directions, and
both landed where predicted.

**Under-report** — `ICCBasedColorSpaceDetails.VariesByRenderingIntent => false`, i.e. §5's "a composite
forgets to forward" failure mode, run over `InitializeColorRenderingIntentTests` +
`VariesByRenderingIntentTests`:

```
Failed: 1, Passed: 24
  InitializeColorRenderingIntentTests.IntentSetAfterADeviceColourOperator_StillApplies_WhenDefaultRgbIsIccBased
```

Exactly one test, and it is the behavioural one — a `/DefaultRGB`-substituted `rg` followed by `ri`. So the
property is load-bearing for `SetDeviceColor` rather than decorative, and the silent-wrong-colour mode this
change could introduce is covered by an assertion that already existed.

**Over-report** — the call site forced to the conservative path (`bool isFixed = false`, i.e. always retain
the operands), full core suite:

```
Passed! - Failed: 0, Passed: 4337, Skipped: 7, Total: 4344
```

Nothing fails. This is the stronger of the two results: taking the safe path everywhere changes no test
outcome, so the property can only remove work and cannot change an answer. A behaviour change would have to
show up as a *difference* between these two runs, and there is none.

**Final state, Release:**

```
dotnet build src/UglyToad.PdfPig                        → 0 errors, all 7 TFMs, no new warnings
dotnet test  src/UglyToad.PdfPig.Tests        (net9.0)  → 4337 passed, 0 failed, 7 skipped
dotnet test  …Rendering.Skia.Tests            (net9.0)  →  542 passed, 0 failed
```

The core figure is **identical to the over-report run above** — 4337 / 0 / 7 both times — which is the
direct evidence rather than an inference: the optimised path and the conservative path produce the same
result on every test in the suite. The Skia figure is unchanged from before the change, so no rendered
pixel moved either; that suite includes the ICC and output-intent goldens, which are the ones that go
through an intent-varying colour space.

The 4337 is 4323 before this change plus the 14 new tests, so nothing that previously passed was disturbed.
