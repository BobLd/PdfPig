using System;
using System.Diagnostics;
using UglyToad.PdfPig;

// Times a single GetPage(1) call and reports the allocation volume it caused.
// Allocation is the interesting number here: the slowdown in #1390 is dominated by
// GC pressure from repeatedly re-materialising the same resource dictionary.
//
//   dotnet run -c Release --project repro/Repro.csproj -- documents/<file>.pdf

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: Repro <path-to-pdf>");
    return 1;
}

var sw = Stopwatch.StartNew();
using var document = PdfDocument.Open(args[0], new ParsingOptions { UseLenientParsing = true });
Console.WriteLine($"Open:    {sw.ElapsedMilliseconds} ms");

var allocatedBefore = GC.GetTotalAllocatedBytes(true);
var (gen0, gen1, gen2) = (GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));

sw.Restart();
var page = document.GetPage(1);
sw.Stop();

var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;

Console.WriteLine($"GetPage: {sw.ElapsedMilliseconds} ms, paths={page.Paths.Count}, letters={page.Letters.Count}");
Console.WriteLine($"         allocated {allocated / (1024.0 * 1024.0):F1} MB, "
                  + $"GC gen0/1/2: {GC.CollectionCount(0) - gen0}/{GC.CollectionCount(1) - gen1}/{GC.CollectionCount(2) - gen2}");
return 0;
