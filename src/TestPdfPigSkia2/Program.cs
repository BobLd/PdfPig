// See https://aka.ms/new-console-template for more information
using UglyToad.PdfPig;

Console.WriteLine("Start!");

using (var document = PdfDocument.Open(@"C:\Users\Bob\Documents\Pdf\Pdf 2.0\全国临床检验操作规程（第4版）.pdf"))
{
    while (true)
    {
        for (int p = 1; p <= document.NumberOfPages; p++)
        {
            var page = document.GetPage(p);
            Console.WriteLine($"Rendered page {p} [{page.Number}].");
        }
    }
}

Console.WriteLine("Done!");
Console.ReadKey();