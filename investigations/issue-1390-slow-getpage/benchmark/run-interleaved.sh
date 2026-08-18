#!/usr/bin/env bash
# Interleaved rounds, but with several reps inside each process so the minimum is JIT-warm.
set -euo pipefail
S="/c/Users/Bob/AppData/Local/Temp/claude/C--Users-Bob-source-repos-BobLd-PdfPig-src/e9209a01-45d2-4c38-b7d1-279f3c5dfb4e/scratchpad"
CLONE="$S/bench-clone"; BUILDS="$S/builds"; OUT="$S/results4.csv"
CORPUS="$CLONE/src/UglyToad.PdfPig.Tests/Integration/Documents"
REPRO="/c/Users/Bob/source/repos/BobLd/PdfPig/investigations/issue-1390-slow-getpage/documents"

: > "$OUT"
echo "variant,mode,document,rep,pages,ms,allocatedBytes,gen0,gen1,gen2,status" >> "$OUT"

ROUNDS=${1:-3}
for round in $(seq 1 "$ROUNDS"); do
  for v in baseline scanner-only scanner+form a-plus-b full; do
    dotnet "$BUILDS/$v/Bench.dll" "$v" "$(cygpath -w "$OUT")" corpus "$(cygpath -w "$CORPUS")" 3 5
  done
  echo "round $round done"
done

for round in $(seq 1 2); do
  for v in scanner-only scanner+form a-plus-b full; do
    dotnet "$BUILDS/$v/Bench.dll" "$v" "$(cygpath -w "$OUT")" single "$(cygpath -w "$REPRO")" 2
  done
  echo "single round $round done"
done
echo "=== done -> $OUT ==="
