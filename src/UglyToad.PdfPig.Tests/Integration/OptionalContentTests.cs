namespace UglyToad.PdfPig.Tests.Integration
{
    public class OptionalContentTests
    {
        [Fact]
        public void NoMarkedOptionalContent()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("AcroFormsBasicFields.pdf")))
            {
                var page = document.GetPage(1);
                var oc = page.GetOptionalContents();

                Assert.Empty(oc);
            }
        }

        [Fact]
        public void MarkedOptionalContent()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("odwriteex.pdf")))
            {
                var page = document.GetPage(1);
                var oc = page.GetOptionalContents();

                Assert.Equal(3, oc.Count);

                Assert.Contains("0", oc);
                Assert.Contains("Dimentions", oc);
                Assert.Contains("Text", oc);

                Assert.Single(oc["0"]);
                Assert.Equal(2, oc["Dimentions"].Count);
                Assert.Single(oc["Text"]);
            }
        }

        [Fact]
        public void MarkedOptionalContentRecursion()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Layer pdf - 322_High_Holborn_building_Brochure.pdf")))
            {
                var page1 = document.GetPage(1);
                var oc1 = page1.GetOptionalContents();
                Assert.Equal(16, oc1.Count);
                Assert.Contains("NEW ARRANGEMENT", oc1);

                var page2 = document.GetPage(2);
                var oc2 = page2.GetOptionalContents();
                Assert.Equal(15, oc2.Count);
                Assert.DoesNotContain("NEW ARRANGEMENT", oc2);
                Assert.Contains("WDL Shell text", oc2);
                Assert.Equal(2, oc2["WDL Shell text"].Count);

                var page3 = document.GetPage(3);
                var oc3 = page3.GetOptionalContents();
                Assert.Equal(15, oc3.Count);
                Assert.Contains("WDL Shell text", oc3);
                Assert.Equal(2, oc3["WDL Shell text"].Count);
            }
        }

        // Ghent Workgroup optional content test files: the default configuration shows the "Default View"
        // layer only. The hidden "GWG View 1" / "GWG View 2" layers each carry a label and the paths of a
        // tick, drawn mirrored so that together with the visible tick they form an X. The visible paragraph
        // also quotes the layer names, so the labels are counted rather than looked for.
        // GWG150: /D with alternate /Configs; GWG151: radio-button group; GWG152: membership dictionaries.
        [Theory]
        [InlineData("GWG150_OptionalContent-OCCD_X4", 11, 6)]
        [InlineData("GWG151_OptionalContent-RBGroup_X4", 10, 6)]
        [InlineData("GWG152_OptionalContent-OCMD_X4", 11, 6)]
        public void SkipHiddenOptionalContent(string document, int allPaths, int visiblePaths)
        {
            var path = IntegrationHelpers.GetDocumentPath(document);

            int allLabels;
            using (var all = PdfDocument.Open(path))
            {
                // Off by default: hidden content is still returned.
                var page = all.GetPage(1);

                Assert.Contains("Default View", page.Text);
                Assert.EndsWith("GWG View 1GWG View 2", page.Text);
                Assert.Equal(allPaths, page.Paths.Count);
                allLabels = CountOccurrences(page.Text, "GWG View 1");
            }

            using (var visible = PdfDocument.Open(path, new ParsingOptions { SkipHiddenOptionalContent = true }))
            {
                var page = visible.GetPage(1);

                Assert.Contains("Default View", page.Text);
                Assert.DoesNotContain("GWG View 1GWG View 2", page.Text);
                Assert.Equal(allLabels - 1, CountOccurrences(page.Text, "GWG View 1"));
                Assert.Equal(visiblePaths, page.Paths.Count);
            }
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            for (int i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }

        [Fact]
        public void SetOptionalContentChangesWhatPagesProcessedAfterwardsContain()
        {
            // GWG 15.1: "Default", "GWG View 1" and "GWG View 2" form a radio-button group; Default is ON.
            var path = IntegrationHelpers.GetDocumentPath("GWG151_OptionalContent-RBGroup_X4");

            using var document = PdfDocument.Open(path, new ParsingOptions { SkipHiddenOptionalContent = true });

            var state = document.OptionalContent;
            Assert.NotNull(state);
            Assert.Equal(new[] { "Default", "GWG View 1", "GWG View 2" }, state.Order.Select(n => n.Group!.Name));

            var before = document.GetPage(1).Text;
            int viewOneBefore = CountOccurrences(before, "GWG View 1");
            Assert.Contains("Default View", before);

            var viewOne = state.Groups.Single(g => g.Name == "GWG View 1");
            document.SetOptionalContent(state.WithGroupState(viewOne, true));

            Assert.False(document.OptionalContent!.IsOn(state.Groups.Single(g => g.Name == "Default")));

            var after = document.GetPage(1).Text;
            Assert.DoesNotContain("Default View", after);
            Assert.Equal(viewOneBefore + 1, CountOccurrences(after, "GWG View 1"));
        }

        [Fact]
        public void SetOptionalContentRejectsAStateOfAnotherDocument()
        {
            var path = IntegrationHelpers.GetDocumentPath("GWG151_OptionalContent-RBGroup_X4");

            using var first = PdfDocument.Open(path);
            using var second = PdfDocument.Open(path);

            Assert.Throws<ArgumentException>(() => first.SetOptionalContent(second.OptionalContent!));
        }

        [Fact]
        public void SetOptionalContentOnADocumentWithoutOptionalContentSaysSo()
        {
            using var withLayers = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("GWG151_OptionalContent-RBGroup_X4"));
            using var withoutLayers = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("AcroFormsBasicFields.pdf"));

            var e = Assert.Throws<ArgumentException>(() => withoutLayers.SetOptionalContent(withLayers.OptionalContent!));
            Assert.StartsWith("The document has no optional content.", e.Message);
        }

        [Fact]
        public void DocumentWithoutOptionalContentHasNoState()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("AcroFormsBasicFields.pdf"));

            Assert.Null(document.OptionalContent);
        }

        [Theory]
        [InlineData("odwriteex.pdf")]
        [InlineData("Layer pdf - 322_High_Holborn_building_Brochure.pdf")]
        public void SkipHiddenOptionalContentNeverAddsContent(string document)
        {
            var path = IntegrationHelpers.GetDocumentPath(document);

            using (var all = PdfDocument.Open(path))
            using (var visible = PdfDocument.Open(path, new ParsingOptions { SkipHiddenOptionalContent = true }))
            {
                for (int p = 1; p <= all.NumberOfPages; p++)
                {
                    var allPage = all.GetPage(p);
                    var visiblePage = visible.GetPage(p);

                    Assert.True(visiblePage.Letters.Count <= allPage.Letters.Count);
                    Assert.True(visiblePage.Paths.Count <= allPage.Paths.Count);
                    Assert.True(visiblePage.GetImages().Count() <= allPage.GetImages().Count());
                }
            }
        }
    }
}
