#!/usr/bin/env bash
# Publishes all five variants side by side, then measures them round-robin so that any drift in
# machine state is shared equally between variants instead of being attributed to one of them.
set -euo pipefail

S="/c/Users/Bob/AppData/Local/Temp/claude/C--Users-Bob-source-repos-BobLd-PdfPig-src/e9209a01-45d2-4c38-b7d1-279f3c5dfb4e/scratchpad"
CLONE="$S/bench-clone"; BENCH="$S/bench"; BUILDS="$S/builds"; OUT="$S/results3.csv"
CORPUS="$CLONE/src/UglyToad.PdfPig.Tests/Integration/Documents"
REPRO="/c/Users/Bob/source/repos/BobLd/PdfPig/investigations/issue-1390-slow-getpage/documents"

BASE=755c1087; AB=6921ae74; FULL=4d50e50d
SCANNER=src/UglyToad.PdfPig/Tokenization/Scanner/PdfTokenScanner.cs
PROCESSOR=src/UglyToad.PdfPig/Graphics/BaseStreamProcessor.cs
COMPARER=src/UglyToad.PdfPig/Util/ReferenceEqualityComparer.cs

rm -rf "$BUILDS"; mkdir -p "$BUILDS"
: > "$OUT"
echo "variant,mode,document,rep,pages,ms,allocatedBytes,gen0,gen1,gen2,status" >> "$OUT"

clean () { ( cd "$CLONE" && git reset -q --hard && git clean -qfd && git checkout -qf --detach "$1" && git reset -q --hard "$1" ); }

publish () {
  local variant="$1"
  echo "publishing $variant"
  dotnet publish "$BENCH/Bench.csproj" -c Release -f net8.0 -o "$BUILDS/$variant" -v q --nologo \
    "-p:BenchLibPath=$(cygpath -w "$CLONE/src/UglyToad.PdfPig/UglyToad.PdfPig.csproj")" >/dev/null
}

clean $BASE;                                                              publish baseline
clean $BASE; ( cd "$CLONE" && git checkout -q $AB -- "$SCANNER" );        publish scanner-only
clean $BASE; ( cd "$CLONE" && git checkout -q $AB -- "$SCANNER" "$COMPARER" \
                 && git checkout -q $FULL -- "$PROCESSOR" ) \
             && python "$S/make-scanner-form.py" "$(cygpath -m "$CLONE")"; publish scanner+form
clean $AB;                                                                publish a-plus-b
clean $FULL;                                                              publish full

ROUNDS=${1:-6}
for round in $(seq 1 "$ROUNDS"); do
  for v in baseline scanner-only scanner+form a-plus-b full; do
    dotnet "$BUILDS/$v/Bench.dll" "$v" "$(cygpath -w "$OUT")" corpus "$(cygpath -w "$CORPUS")" 1 5
  done
  echo "round $round done"
done

# Repro documents: baseline is excluded, one rep takes ~4 minutes on the full-scale file and its
# numbers are already established.
for round in 1 2; do
  for v in scanner-only scanner+form a-plus-b full; do
    dotnet "$BUILDS/$v/Bench.dll" "$v" "$(cygpath -w "$OUT")" single "$(cygpath -w "$REPRO")" 1
  done
  echo "single round $round done"
done

echo "=== done -> $OUT ==="
