"""Reproducibility of the corpus statistic, and a paired test between variants.

Each (variant, round) is one process that ran the corpus 3 times. Rep 1 is cold (JIT), so the
per-round statistic is min(rep2, rep3). Spread of that statistic across the 3 rounds is the real
reproducibility of the number the comparison rests on.
"""
import csv, sys, statistics, collections, itertools, math

PATH = sys.argv[1]
V = ["baseline", "scanner-only", "scanner+form", "a-plus-b", "full"]

# rows arrive grouped by process: 3 reps x 150 docs, repeated per round
per_round = collections.defaultdict(list)   # (variant, doc) -> [round-statistic, ...]
buf = collections.defaultdict(lambda: collections.defaultdict(list))  # variant -> doc -> [(rep, ms)]

rows = list(csv.DictReader(open(PATH, newline="")))
rows = [r for r in rows if r["mode"] == "corpus"]

# Split into processes: a new process starts when we see rep 1 for the first document again.
proc = collections.defaultdict(list)
seen = collections.defaultdict(int)
for r in rows:
    v = r["variant"]
    key = (v, r["document"], r["rep"])
    seen[key] += 1
    proc[(v, seen[key])].append(r)

for (v, round_no), rs in proc.items():
    by_doc = collections.defaultdict(list)
    for r in rs:
        by_doc[r["document"]].append((int(r["rep"]), float(r["ms"])))
    for d, xs in by_doc.items():
        warm = [ms for rep, ms in xs if rep >= 2] or [ms for _, ms in xs]
        per_round[(v, d)].append(min(warm))

docs = sorted({d for (_, d) in per_round})
measurable = [d for d in docs if min(per_round[("baseline", d)]) >= 5.0]

print("REPRODUCIBILITY of the per-round warm statistic")
sp = []
for v in V:
    for d in measurable:
        xs = per_round[(v, d)]
        if len(xs) >= 2 and min(xs) > 0:
            sp.append((max(xs) - min(xs)) / min(xs))
sp.sort()
print(f"  rounds per variant/document: {len(per_round[('baseline', measurable[0])])}")
print(f"  (max-min)/min across rounds: median {statistics.median(sp):.1%}, "
      f"p75 {sp[3*len(sp)//4]:.1%}, p90 {sp[int(0.9*len(sp))]:.1%}")

# Reproducibility of the corpus TOTAL, which is the headline number.
print("\nCorpus total per round (ms) - this is the number the comparison rests on")
n_rounds = min(len(per_round[(v, d)]) for v in V for d in docs)
print(f"{'variant':<16}" + "".join(f"{'round '+str(i+1):>10}" for i in range(n_rounds)) + f"{'min':>10}{'spread':>9}")
totals = {}
for v in V:
    ts = [sum(per_round[(v, d)][i] for d in docs) for i in range(n_rounds)]
    totals[v] = ts
    print(f"{v:<16}" + "".join(f"{t:>10.0f}" for t in ts) + f"{min(ts):>10.0f}{(max(ts)-min(ts))/min(ts):>8.1%}")

print("\nBetween-variant differences vs within-variant round-to-round spread:")
best_tot = {v: min(totals[v]) for v in V}
worst_spread = max((max(totals[v]) - min(totals[v])) / min(totals[v]) for v in V)
rng = max(best_tot.values()) - min(best_tot.values())
print(f"  spread of best totals across variants: {rng:.0f} ms "
      f"({rng/min(best_tot.values()):.1%} of the fastest)")
print(f"  largest within-variant round-to-round spread: {worst_spread:.1%}")
print("  -> the variant differences are " +
      ("NOT resolvable" if rng/min(best_tot.values()) <= worst_spread else "larger than the noise"))

print("\nPaired sign test on per-document warm minima (documents with baseline >= 5 ms)")
print(f"{'comparison':<40}{'faster':>8}{'slower':>8}{'|d|>5%':>9}{'median ratio':>14}{'p':>10}")
def sign_test(a, b):
    """b relative to a."""
    fa = fb = 0
    ratios = []
    for d in measurable:
        x, y = min(per_round[(a, d)]), min(per_round[(b, d)])
        ratios.append(y / x)
        if y < x: fb += 1
        else: fa += 1
    n = fa + fb
    k = min(fa, fb)
    # two-sided binomial p
    p = 2 * sum(math.comb(n, i) for i in range(k + 1)) / 2 ** n
    big = sum(1 for r in ratios if abs(r - 1) > 0.05)
    return fb, fa, big, statistics.median(ratios), min(p, 1.0)

for a, b in [("baseline", "scanner-only"), ("baseline", "scanner+form"),
             ("baseline", "a-plus-b"), ("baseline", "full"),
             ("scanner-only", "a-plus-b"), ("scanner+form", "full"),
             ("scanner-only", "scanner+form"), ("a-plus-b", "full")]:
    f, s, big, med, p = sign_test(a, b)
    print(f"{b+' vs '+a:<40}{f:>8}{s:>8}{big:>9}{med:>14.3f}{p:>10.3f}")
