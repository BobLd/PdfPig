using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;

// Bench <variant> <outCsv> <mode> <pathOrDir> <reps> [maxPagesPerDoc]
//   mode "single": <pathOrDir> is a directory of pdfs, GetPage(1) only
//   mode "corpus": <pathOrDir> is a directory of pdfs, every page up to maxPagesPerDoc
//
// Every document is run <reps> times; each run is a CSV row and the analysis takes the minimum
// per (variant, document). Run 1 doubles as JIT and OS-file-cache warm-up.
//
// CSV: variant,mode,document,rep,pages,ms,allocatedBytes,gen0,gen1,gen2,status

var variant = args[0];
var outCsv = args[1];
var mode = args[2];
var target = args[3];
var reps = int.Parse(args[4], CultureInfo.InvariantCulture);
var maxPages = args.Length > 5 ? int.Parse(args[5], CultureInfo.InvariantCulture) : int.MaxValue;

var pageLimit = mode == "single" ? 1 : maxPages;
var files = Directory.GetFiles(target, "*.pdf").OrderBy(x => x, StringComparer.Ordinal).ToList();

var rows = new List<string>();

for (var rep = 1; rep <= reps; rep++)
{
    foreach (var file in files)
    {
        rows.Add(Measure(file, pageLimit, rep));
    }
}

File.AppendAllLines(outCsv, rows);

string Measure(string path, int limit, int rep)
{
    var name = Path.GetFileName(path);

    var allocatedBefore = GC.GetTotalAllocatedBytes(true);
    var g0 = GC.CollectionCount(0);
    var g1 = GC.CollectionCount(1);
    var g2 = GC.CollectionCount(2);

    var sw = Stopwatch.StartNew();
    var pages = 0;

    try
    {
        using var document = PdfDocument.Open(path, new ParsingOptions { UseLenientParsing = true });
        var count = Math.Min(document.NumberOfPages, limit);
        for (var i = 1; i <= count; i++)
        {
            var page = document.GetPage(i);
            if (page.Letters.Count >= 0)
            {
                pages++;
            }
        }
    }
    catch (Exception e)
    {
        sw.Stop();
        return Row(name, rep, pages, sw.Elapsed.TotalMilliseconds, 0, 0, 0, 0, "failed:" + e.GetType().Name);
    }

    sw.Stop();

    var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;

    return Row(name,
        rep,
        pages,
        sw.Elapsed.TotalMilliseconds,
        allocated,
        GC.CollectionCount(0) - g0,
        GC.CollectionCount(1) - g1,
        GC.CollectionCount(2) - g2,
        "ok");
}

string Row(string name, int rep, int pages, double ms, long allocated, int g0, int g1, int g2, string status)
    => string.Join(",",
        variant,
        mode,
        name,
        rep.ToString(CultureInfo.InvariantCulture),
        pages.ToString(CultureInfo.InvariantCulture),
        ms.ToString("F1", CultureInfo.InvariantCulture),
        allocated.ToString(CultureInfo.InvariantCulture),
        g0.ToString(CultureInfo.InvariantCulture),
        g1.ToString(CultureInfo.InvariantCulture),
        g2.ToString(CultureInfo.InvariantCulture),
        status);
