namespace UglyToad.PdfPig.Tests.Integration
{
    using PdfPig.Tokens;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Issue823
    {
        [Fact]
        public void CanGetPages()
        {
            var path = IntegrationHelpers.GetDocumentPath("LUKB-Anlagepolitik-Q4-2023.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions()
                   {
                       SkipMissingFonts = true
                   }))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i+1);
                }
            }
        }
    }
}
