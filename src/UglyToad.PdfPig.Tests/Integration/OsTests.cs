﻿using System.Collections.Generic;
using UglyToad.PdfPig.Tests.Dla;
using Xunit;

namespace UglyToad.PdfPig.Tests.Integration
{
    public class OsTests
    {
        public static IEnumerable<object[]> DataExtract => new[]
        {
            new object[]
            {
                "90 180 270 rotated.pdf",
                new object[][]
                {
                    new object[] { "[(x:53.88, y:759.48), 2.495859375, 0]", 0.0 },
                    new object[] { "[(x:514.925312502883, y:744.099765720344), 6.83203125, 7.94531249999983]", -90.0 },
                    new object[] { "[(x:512.505390717836, y:736.603703191305), 5.1796875, 5.68945312499983]", -90.0 },
                    new object[] { "[(x:512.505390785898, y:730.931828191305), 3.99609375, 5.52539062499994]", -90.0 },
                }
            },
        };

        [Theory]
        [MemberData(nameof(DataExtract))]
        public void GetCorrectBBoxLinux(string name, object[][] expected)
        {
            // success on windows
            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath(name)))
            {
                var page = document.GetPage(1);

                for (int i = 0; i < expected.Length; i++)
                {
                    string bbox = (string)expected[i][0];
                    var rotation = (double)expected[i][1];
                    var current = page.Letters[i];
                    Assert.Equal(bbox, current.GlyphRectangle.ToString());
                    Assert.Equal(rotation, current.GlyphRectangle.Rotation, 3);
                }
            }
        }
    }
}
