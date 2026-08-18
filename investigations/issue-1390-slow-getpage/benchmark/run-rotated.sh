#!/usr/bin/env bash
# Rotates the variant order every round so that no variant is systematically last. A fixed order
# penalises whichever variant runs at the end of a round, which is what made the earlier
# "cause A costs 2%" result appear.
set -euo pipefail
S="/c/Users/Bob/AppData/Local/Temp/claude/C--Users-Bob-source-repos-BobLd-PdfPig-src/e9209a01-45d2-4c38-b7d1-279f3c5dfb4e/scratchpad"
BUILDS="$S/builds"; OUT="$S/results6.csv"; CORPUS="$S/corpus-fast"
REPRO="/c/Users/Bob/source/repos/BobLd/PdfPig/investigations/issue-1390-slow-getpage/documents"

V=(baseline scanner+form full full-v2)
N=${#V[@]}

: > "$OUT"
echo "variant,mode,document,rep,pages,ms,allocatedBytes,gen0,gen1,gen2,status" >> "$OUT"

ROUNDS=${1:-8}
for r in $(seq 0 $((ROUNDS-1))); do
  for i in $(seq 0 $((N-1))); do
    v=${V[$(( (i + r) % N ))]}
    dotnet "$BUILDS/$v/Bench.dll" "$v" "$(cygpath -w "$OUT")" corpus "$(cygpath -w "$CORPUS")" 3 5
  done
  echo "round $((r+1)) done"
done

for r in 0 1 2; do
  for i in $(seq 0 $((N-1))); do
    v=${V[$(( (i + r) % N ))]}
    [ "$v" = "baseline" ] && continue
    dotnet "$BUILDS/$v/Bench.dll" "$v" "$(cygpath -w "$OUT")" single "$(cygpath -w "$REPRO")" 2
  done
  echo "single round $((r+1)) done"
done
echo "=== done -> $OUT ==="
