# Benchmark data for COST-BENEFIT.md

Raw measurements and the scripts that produced them.

| file | what it is |
| --- | --- |
| `corpus-and-repro.csv` | every timing/allocation measurement: 5 variants x (150 corpus documents + 13 reproduction documents), 3 warm repetitions per process, 3 interleaved rounds |
| `memory.csv` | retained managed heap and peak working set, one process per document |
| `Bench.cs` / `MemBench.cs` | the two harnesses (each needs a csproj with a `BenchLibPath` ProjectReference to the variant under test) |
| `publish-variants.sh` | checks out and publishes all five variants side by side from a throwaway clone |
| `run-interleaved.sh` | measures the published variants round-robin |
| `make-scanner-form.py` | constructs the `scanner+form` variant, which has no corresponding commit |
| `report.py` | the tables in COST-BENEFIT.md |
| `stats.py` | reproducibility check and the paired sign test |

CSV columns: `variant,mode,document,rep,pages,ms,allocatedBytes,gen0,gen1,gen2,status`.
Repetition 1 of each process is cold (JIT) and is discarded by `stats.py`; `report.py` takes the
minimum across all rows, which selects a warm run.

The variants are pinned to commits `755c1087` (baseline), `6921ae74` (causes A+B) and `4d50e50d`
(everything). `scanner-only` and `scanner+form` are assembled from individual files of those
commits — see `publish-variants.sh`.

Reproducing needs a clone of this repository at those commits; the scripts hardcode a scratch
path and will need that adjusted.
