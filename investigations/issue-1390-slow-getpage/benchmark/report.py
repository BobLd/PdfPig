"""Analysis of the interleaved sweep (results3.csv): six round-robin rounds over 150 corpus
documents, five variants, plus the reproduction documents."""
import csv, sys, statistics, collections

PATH = sys.argv[1]
OLD = sys.argv[2]          # first sweep, only for the baseline reproduction-document rows
V = ["baseline", "scanner-only", "scanner+form", "a-plus-b", "full"]

runs = collections.defaultdict(list)
with open(PATH, newline="") as fh:
    for r in csv.DictReader(fh):
        runs[(r["variant"], r["mode"], r["document"])].append((float(r["ms"]), int(r["allocatedBytes"])))

base_single = {}
with open(OLD, newline="") as fh:
    for r in csv.DictReader(fh):
        if r["variant"] == "baseline" and r["mode"] == "single":
            d = r["document"]
            ms = float(r["ms"])
            if d not in base_single or ms < base_single[d][0]:
                base_single[d] = (ms, int(r["allocatedBytes"]))

def bestms(v, mode, d):
    xs = runs.get((v, mode, d))
    return min(x[0] for x in xs) if xs else None

def bestmb(v, mode, d):
    xs = runs.get((v, mode, d))
    return min(x[1] for x in xs) / 1048576 if xs else None

cdocs = sorted({d for (v, m, d) in runs if m == "corpus"})
measurable = [d for d in cdocs if bestms("baseline", "corpus", d) >= 5.0]

print("=" * 108)
print("MEASUREMENT NOISE  (spread across the 6 interleaved rounds, per document)")
print("=" * 108)
spreads = []
for v in V:
    for d in measurable:
        xs = sorted(x[0] for x in runs[(v, "corpus", d)])
        spreads.append((xs[-1] - xs[0]) / xs[0])
spreads.sort()
print(f"  max-min as a fraction of the min: median {statistics.median(spreads):.1%}, "
      f"p75 {spreads[3*len(spreads)//4]:.1%}, p95 {spreads[int(0.95*len(spreads))]:.1%}")
print("  -> corpus differences smaller than this are not resolvable.\n")

print("=" * 108)
print("REAL TEST CORPUS  —  150 documents, up to 5 pages each, best of 6 interleaved rounds")
print("=" * 108)
print(f"{'variant':<16}{'total ms':>10}{'vs base':>10}{'median':>9}{'p25':>8}{'p75':>8}"
      f"{'>5% faster':>12}{'>5% slower':>12}{'total MB':>10}")
base_tot = sum(bestms("baseline", "corpus", d) for d in cdocs)
for v in V:
    tot = sum(bestms(v, "corpus", d) for d in cdocs)
    mb = sum(bestmb(v, "corpus", d) for d in cdocs)
    ratios = sorted(bestms(v, "corpus", d) / bestms("baseline", "corpus", d) for d in measurable)
    print(f"{v:<16}{tot:>10.0f}{tot/base_tot*100:>9.1f}%{statistics.median(ratios):>9.3f}"
          f"{ratios[len(ratios)//4]:>8.3f}{ratios[3*len(ratios)//4]:>8.3f}"
          f"{sum(1 for r in ratios if r < 0.95):>12}{sum(1 for r in ratios if r > 1.05):>12}{mb:>10.0f}")
print(f"({len(measurable)} of {len(cdocs)} documents have a baseline >= 5 ms; ratios use only those)")

print()
print("=" * 108)
print("COST OF EACH STEP ON THE CORPUS  (paired, same rounds)")
print("=" * 108)
for without, with_step, label in [
        ("baseline", "scanner-only", "scanner cache (cause B)"),
        ("scanner-only", "scanner+form", "form-stream cache"),
        ("scanner+form", "full", "resource memoization (cause A)"),
        ("scanner-only", "a-plus-b", "resource memoization (cause A), no form fix")]:
    ratios = sorted(bestms(with_step, "corpus", d) / bestms(without, "corpus", d) for d in measurable)
    tw = sum(bestms(without, "corpus", d) for d in cdocs)
    ta = sum(bestms(with_step, "corpus", d) for d in cdocs)
    print(f"  {label:<45} median {statistics.median(ratios):.3f}  total {tw:.0f} -> {ta:.0f} ms ({(ta/tw-1)*100:+.1f}%)")

print()
print("=" * 108)
print("REPRODUCTION DOCUMENTS  —  Open + GetPage(1), ms (best run)")
print("=" * 108)
sdocs = sorted({d for (v, m, d) in runs if m == "single"})
print(f"{'document':<42}" + "".join(f"{v:>14}" for v in V))
print("-" * 108)
for d in sdocs:
    line = f"{d[:40]:<42}"
    b = base_single.get(d)
    line += f"{b[0]:>14,.0f}" if b else f"{'-':>14}"
    for v in V[1:]:
        line += f"{bestms(v, 'single', d):>14,.1f}"
    print(line)

print()
print("=" * 108)
print("THE REPORTED DOCUMENT SHAPE, STEP BY STEP")
print("=" * 108)
for d in ["fullscale-extgstate-30k-4200do.pdf", "formstream-196kb-200do.pdf"]:
    print(f"\n  {d}")
    b = base_single[d]
    print(f"    {'baseline':<14}{b[0]:>13,.1f} ms{b[1]/1048576:>12,.1f} MB")
    for v in V[1:]:
        print(f"    {v:<14}{bestms(v,'single',d):>13,.1f} ms{bestmb(v,'single',d):>12,.1f} MB")
