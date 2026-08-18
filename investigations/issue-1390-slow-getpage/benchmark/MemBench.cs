using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;

// MemBench <variant> <outCsv> <pdf> [maxPages]
// Reports memory still held while the document is open, which is the price paid for the caches:
//   retainedBytes  managed heap after a full blocking collection, document still alive
//   peakWorkingSet process peak, includes everything
// CSV: variant,document,pages,retainedBytes,peakWorkingSetBytes

var variant = args[0];
var outCsv = args[1];
var path = args[2];
var maxPages = args.Length > 3 ? int.Parse(args[3], CultureInfo.InvariantCulture) : int.MaxValue;

int pages;
long retained;

using (var document = PdfDocument.Open(path, new ParsingOptions { UseLenientParsing = true }))
{
    var count = Math.Min(document.NumberOfPages, maxPages);
    pages = 0;
    for (var i = 1; i <= count; i++)
    {
        var page = document.GetPage(i);
        if (page.Letters.Count >= 0)
        {
            pages++;
        }
    }

    // Document is still in scope, so anything the caches hold is still rooted.
    retained = GC.GetTotalMemory(true);

    GC.KeepAlive(document);
}

var peak = Process.GetCurrentProcess().PeakWorkingSet64;

File.AppendAllLines(outCsv, new[]
{
    string.Join(",",
        variant,
        Path.GetFileName(path),
        pages.ToString(CultureInfo.InvariantCulture),
        retained.ToString(CultureInfo.InvariantCulture),
        peak.ToString(CultureInfo.InvariantCulture))
});
