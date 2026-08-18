# Benchmark data for COST-BENEFIT.md

Raw measurements and the scripts that produced them.

| file | what it is |
| --- | --- |
| `rotated-order.csv` | **the sweep the conclusions rest on**: 4 variants x 8 rounds x 3 repetitions, variant order rotated each round, 69 corpus documents + 13 reproduction documents |
| `corpus-and-repro.csv` | earlier sweep, 5 variants x 150 corpus documents, fixed variant order — retained because the retracted "cause A costs 2%" result came from it |
| `fixed-order-v2.csv` | the sweep that exposed the ordering bias: adding `full-v2` at the end of the order transferred the penalty to it |
| `memory.csv` | retained managed heap and peak working set, one process per document |
| `Bench.cs` / `MemBench.cs` | the two harnesses (each needs a csproj with a `BenchLibPath` ProjectReference to the variant under test) |
| `publish-variants.sh` | checks out and publishes all five variants side by side from a throwaway clone |
| `run-interleaved.sh` | measures the published variants round-robin, fixed order (superseded) |
| `run-rotated.sh` | measures them with the order rotated each round — use this one |
| `make-scanner-form.py` | constructs the `scanner+form` variant, which has no corresponding commit |
| `report.py` | the tables in COST-BENEFIT.md |
| `stats.py` / `stats-rotated.py` | reproducibility check and the paired sign test |

CSV columns: `variant,mode,document,rep,pages,ms,allocatedBytes,gen0,gen1,gen2,status`.
Repetition 1 of each process is cold (JIT) and is discarded by `stats.py`; `report.py` takes the
minimum across all rows, which selects a warm run.

The variants are pinned to commits `755c1087` (baseline), `6921ae74` (causes A+B) and `4d50e50d`
(everything). `scanner-only` and `scanner+form` are assembled from individual files of those
commits — see `publish-variants.sh`.

Reproducing needs a clone of this repository at those commits; the scripts hardcode a scratch
path and will need that adjusted.


## A warning about the timing data

Per-document round-to-round spread on the machine used here is around 30%. The corpus *totals*
cannot resolve differences between variants at all, and even the paired sign test cannot resolve
an effect below roughly 2–3%. Three separate methodological faults — cold JIT, drift between
sweeps, and fixed variant ordering — each produced a confident but wrong result before being
found; the last one is documented in COST-BENEFIT.md because a published claim had to be
withdrawn over it.

The allocation columns have none of these problems: they are exact, deterministic, and identical
across every sweep. Prefer them when the timing data is ambiguous.
