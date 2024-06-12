namespace UglyToad.PdfPig.Tests.Integration
{
    using AcroForms.Fields;

    public class AcroFormsBasicFieldsTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("AcroFormsBasicFields");
        }

        [Fact]
        public void Issue848()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("test_doc_issue_848"), ParsingOptions.LenientParsingOff))
            {
                document.TryGetForm(out var form);

                foreach (var field in form.Fields)
                {
                    var str = GetText(field).ToArray();
                }

                Assert.NotNull(form);
            }
        }

        private static IEnumerable<string> GetText(AcroFieldBase acro, string text = null)
        {
            if (text is null)
            {
                text = acro.Information.PartialName;
            }
            else
            {
                text += "." + acro.Information.PartialName;
            }

            if (acro is AcroNonTerminalField nonTerminal)
            {
                foreach (var child in nonTerminal.Children)
                {
                    foreach (var t in GetText(child, text))
                    {
                        yield return t;
                    }
                }
            }
            else if (acro.Information.Parent.HasValue)
            {
               yield return text; // final
            }
        }

        [Fact]
        public void TryGetFormNotNull()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                document.TryGetForm(out var form);
                Assert.NotNull(form);
            }
        }

        [Fact]
        public void TryGetFormDisposedThrows()
        {
            var document = PdfDocument.Open(GetFilename());

            document.Dispose();

            Action action = () => document.TryGetForm(out _);

            Assert.Throws<ObjectDisposedException>(action);
        }

        [Fact]
        public void TryGetGetsAllFormFields()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                document.TryGetForm(out var form);
                Assert.Equal(18, form.Fields.Count);
            }
        }

        [Fact]
        public void TryGetFormFieldsByPage()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                document.TryGetForm(out var form);
                var fields = form.GetFieldsForPage(1).ToList();
                Assert.Equal(18, fields.Count);
            }
        }

        [Fact]
        public void TryGetGetsRadioButtonState()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                document.TryGetForm(out var form);
                var radioButtons = form.Fields.OfType<AcroRadioButtonsField>().ToList();

                Assert.Equal(2, radioButtons.Count);

                // ReSharper disable once PossibleInvalidOperationException
                var ordered = radioButtons.OrderBy(x => x.Children.Min(y => y.Bounds.Value.Left)).ToList();

                var left = ordered[0];

                Assert.Equal(2, left.Children.Count);
                foreach (var acroFieldBase in left.Children)
                {
                    var button = Assert.IsType<AcroRadioButtonField>(acroFieldBase);
                    Assert.False(button.IsSelected);
                }

                var right = ordered[1];
                Assert.Equal(2, right.Children.Count);

                var buttonOn = Assert.IsType<AcroRadioButtonField>(right.Children[0]);
                Assert.True(buttonOn.IsSelected);

                var buttonOff = Assert.IsType<AcroRadioButtonField>(right.Children[1]);
                Assert.False(buttonOff.IsSelected);
            }
        }
    }
}
