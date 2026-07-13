namespace UglyToad.PdfPig.Tests.Graphics
{
    using PdfPig.Content;
    using PdfPig.Graphics.Colors;
    using PdfPig.Tokens;
    using System.Collections.Generic;
    using PdfPig.Functions;
    using UglyToad.PdfPig.Tests.Tokens;

    public class ColorSpaceDetailsTests
    {
        [Fact]
        public void DeviceGrayEquals()
        {
            var cs1 = DeviceGrayColorSpaceDetails.Instance;
            var cs2 = DeviceGrayColorSpaceDetails.Instance;
            Assert.Equal(cs1, cs2);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
        }

        [Fact]
        public void DeviceRgbEquals()
        {
            var cs1 = DeviceRgbColorSpaceDetails.Instance;
            var cs2 = DeviceRgbColorSpaceDetails.Instance;
            Assert.Equal(cs1, cs2);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
        }

        [Fact]
        public void DeviceCmykEquals()
        {
            var cs1 = DeviceCmykColorSpaceDetails.Instance;
            var cs2 = DeviceCmykColorSpaceDetails.Instance;
            Assert.Equal(cs1, cs2);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
        }

        [Fact]
        public void IndexedEquals()
        {
            var baseCs = DeviceRgbColorSpaceDetails.Instance;
            var table1 = new byte[] { 0, 0, 0, 255, 255, 255 };
            var table2 = new byte[] { 0, 0, 0, 255, 255, 255 };
            var table3 = new byte[] { 1, 1, 1, 255, 255, 255 };

            var cs1 = new IndexedColorSpaceDetails(baseCs, 1, table1);
            var cs2 = new IndexedColorSpaceDetails(baseCs, 1, table2);
            var cs3 = new IndexedColorSpaceDetails(baseCs, 1, table3);

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
            Assert.NotEqual(cs1, cs3);
        }

        [Fact]
        public void DeviceColorSpacesNotEqualAcrossTypes()
        {
            Assert.False(DeviceGrayColorSpaceDetails.Instance.Equals(DeviceRgbColorSpaceDetails.Instance));
            Assert.False(DeviceRgbColorSpaceDetails.Instance.Equals(DeviceCmykColorSpaceDetails.Instance));
            Assert.False(DeviceCmykColorSpaceDetails.Instance.Equals(DeviceGrayColorSpaceDetails.Instance));
            Assert.False(DeviceGrayColorSpaceDetails.Instance.Equals(null));
        }

        [Fact]
        public void SeparationEquals()
        {
            var name = NameToken.Create("MySpot");
            var alt = DeviceRgbColorSpaceDetails.Instance;
            var func = CreateTestFunction();

            var cs1 = new SeparationColorSpaceDetails(name, alt, func);
            var cs2 = new SeparationColorSpaceDetails(name, alt, func);
            var cs3 = new SeparationColorSpaceDetails(NameToken.Create("Other"), alt, func);

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
            Assert.NotEqual(cs1, cs3);
        }

        [Fact]
        public void SeparationEqualsWithDistinctButEqualComponents()
        {
            var cs1 = new SeparationColorSpaceDetails(NameToken.Create("MySpot"), DeviceRgbColorSpaceDetails.Instance, CreateTestFunction());
            var cs2 = new SeparationColorSpaceDetails(NameToken.Create("MySpot"), DeviceRgbColorSpaceDetails.Instance, CreateTestFunction());

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs2, cs1);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
            Assert.False(cs1.Equals(null));
        }

        [Fact]
        public void SeparationNotEqualWhenTintFunctionDiffers()
        {
            var cs1 = new SeparationColorSpaceDetails(NameToken.Create("MySpot"), DeviceRgbColorSpaceDetails.Instance, CreateTestFunction(n: 1.0));
            var cs2 = new SeparationColorSpaceDetails(NameToken.Create("MySpot"), DeviceRgbColorSpaceDetails.Instance, CreateTestFunction(n: 2.0));

            Assert.NotEqual(cs1, cs2);
        }

        [Fact]
        public void DeviceNEquals()
        {
            var names = new[] { NameToken.Create("C1"), NameToken.Create("C2") };
            var alt = DeviceCmykColorSpaceDetails.Instance;
            var func = CreateTestFunction();

            var cs1 = new DeviceNColorSpaceDetails(names, alt, func);
            var cs2 = new DeviceNColorSpaceDetails(names, alt, func);

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
        }

        [Fact]
        public void DeviceNEqualsWithDistinctButEqualComponents()
        {
            var cs1 = new DeviceNColorSpaceDetails(
                new[] { NameToken.Create("C1"), NameToken.Create("C2") },
                DeviceCmykColorSpaceDetails.Instance,
                CreateTestFunction());
            var cs2 = new DeviceNColorSpaceDetails(
                new[] { NameToken.Create("C1"), NameToken.Create("C2") },
                DeviceCmykColorSpaceDetails.Instance,
                CreateTestFunction());

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs2, cs1);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
            Assert.False(cs1.Equals(null));
        }

        [Fact]
        public void DeviceNEqualsWhenBothAttributesAreNull()
        {
            var cs1 = new DeviceNColorSpaceDetails(
                new[] { NameToken.Create("C1") },
                DeviceCmykColorSpaceDetails.Instance,
                CreateTestFunction(),
                attributes: null);
            var cs2 = new DeviceNColorSpaceDetails(
                new[] { NameToken.Create("C1") },
                DeviceCmykColorSpaceDetails.Instance,
                CreateTestFunction(),
                attributes: null);

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs2, cs1);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
        }

        [Fact]
        public void DeviceNNotEqualWhenOnlyOneHasAttributes()
        {
            var attributes = new DeviceNColorSpaceDetails.DeviceNColorSpaceAttributes();

            var withAttributes = new DeviceNColorSpaceDetails(
                new[] { NameToken.Create("C1") },
                DeviceCmykColorSpaceDetails.Instance,
                CreateTestFunction(),
                attributes);
            var withoutAttributes = new DeviceNColorSpaceDetails(
                new[] { NameToken.Create("C1") },
                DeviceCmykColorSpaceDetails.Instance,
                CreateTestFunction(),
                attributes: null);

            Assert.NotEqual(withAttributes, withoutAttributes);
            Assert.NotEqual(withoutAttributes, withAttributes);
        }

        [Fact]
        public void DeviceNGetColorIsCached()
        {
            var cs = new DeviceNColorSpaceDetails(
                new[] { NameToken.Create("C1") },
                DeviceGrayColorSpaceDetails.Instance,
                CreateTestFunction());

            var values = new[] { 0.3 };
            var color1 = cs.GetColor(values);

            // Mutating the caller's array after the call must not corrupt the cache.
            values[0] = 0.9;

            var color2 = cs.GetColor(0.3);

            Assert.Same(color1, color2);
            Assert.NotEqual(color1, cs.GetColor(0.7));
        }

        [Fact]
        public void DeviceNNotEqualWhenNamesOrderDiffers()
        {
            var func = CreateTestFunction();
            var cs1 = new DeviceNColorSpaceDetails(
                new[] { NameToken.Create("C1"), NameToken.Create("C2") },
                DeviceCmykColorSpaceDetails.Instance,
                func);
            var cs2 = new DeviceNColorSpaceDetails(
                new[] { NameToken.Create("C2"), NameToken.Create("C1") },
                DeviceCmykColorSpaceDetails.Instance,
                func);

            Assert.NotEqual(cs1, cs2);
            Assert.NotEqual(cs2, cs1);
        }

        [Fact]
        public void IndexedNotEqualWhenHiValOrBaseDiffers()
        {
            var table = new byte[] { 0, 0, 0, 255, 255, 255 };

            var cs1 = new IndexedColorSpaceDetails(DeviceRgbColorSpaceDetails.Instance, 1, table);
            var differentHiVal = new IndexedColorSpaceDetails(DeviceRgbColorSpaceDetails.Instance, 2, table);
            var differentBase = new IndexedColorSpaceDetails(DeviceCmykColorSpaceDetails.Instance, 1, table);

            Assert.NotEqual(cs1, differentHiVal);
            Assert.NotEqual(cs1, differentBase);
            Assert.False(cs1.Equals(null));
        }

        [Fact]
        public void CalGrayEquals()
        {
            var cs1 = new CalGrayColorSpaceDetails(new[] { 0.9505, 1.0, 1.089 }, new[] { 0.0, 0.0, 0.0 }, 2.2);
            var cs2 = new CalGrayColorSpaceDetails(new[] { 0.9505, 1.0, 1.089 }, new[] { 0.0, 0.0, 0.0 }, 2.2);
            var differentGamma = new CalGrayColorSpaceDetails(new[] { 0.9505, 1.0, 1.089 }, new[] { 0.0, 0.0, 0.0 }, 1.8);
            var differentWhitePoint = new CalGrayColorSpaceDetails(new[] { 0.9, 1.0, 1.089 }, new[] { 0.0, 0.0, 0.0 }, 2.2);

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
            Assert.NotEqual(cs1, differentGamma);
            Assert.NotEqual(cs1, differentWhitePoint);
            Assert.False(cs1.Equals(null));
        }

        [Fact]
        public void CalRGBEquals()
        {
            var whitePoint = new[] { 0.9505, 1.0, 1.089 };
            var gamma = new[] { 2.2, 2.2, 2.2 };
            var matrix = new[] { 0.4124, 0.2126, 0.0193, 0.3576, 0.7152, 0.1192, 0.1805, 0.0722, 0.9505 };

            var cs1 = new CalRGBColorSpaceDetails((double[])whitePoint.Clone(), null, (double[])gamma.Clone(), (double[])matrix.Clone());
            var cs2 = new CalRGBColorSpaceDetails((double[])whitePoint.Clone(), null, (double[])gamma.Clone(), (double[])matrix.Clone());
            var differentGamma = new CalRGBColorSpaceDetails((double[])whitePoint.Clone(), null, new[] { 1.8, 1.8, 1.8 }, (double[])matrix.Clone());

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
            Assert.NotEqual(cs1, differentGamma);
            Assert.False(cs1.Equals(null));
        }

        [Fact]
        public void LabEquals()
        {
            var cs1 = new LabColorSpaceDetails(new[] { 0.9505, 1.0, 1.089 }, null, null);
            var cs2 = new LabColorSpaceDetails(new[] { 0.9505, 1.0, 1.089 }, null, null);
            var differentWhitePoint = new LabColorSpaceDetails(new[] { 0.9, 1.0, 1.089 }, null, null);
            var differentRange = new LabColorSpaceDetails(new[] { 0.9505, 1.0, 1.089 }, null, new[] { -50.0, 50.0, -50.0, 50.0 });

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs2, cs1);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
            Assert.NotEqual(cs1, differentWhitePoint);
            Assert.NotEqual(cs1, differentRange);
            Assert.False(cs1.Equals(null));
        }

        [Fact]
        public void LabAndCalRGBAreNotEqual()
        {
            var lab = new LabColorSpaceDetails(new[] { 0.9505, 1.0, 1.089 }, null, null);
            var calRgb = new CalRGBColorSpaceDetails(new[] { 0.9505, 1.0, 1.089 }, null, null, null);

            Assert.False(lab.Equals(calRgb));
            Assert.False(calRgb.Equals(lab));
        }

        [Fact]
        public void ICCBasedEquals()
        {
            var cs1 = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance, new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, null);
            var cs2 = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance, new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, null);
            var differentComponents = new ICCBasedColorSpaceDetails(1, DeviceGrayColorSpaceDetails.Instance, new[] { 0.0, 1.0 }, null);

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs2, cs1);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
            Assert.NotEqual(cs1, differentComponents);
            Assert.False(cs1.Equals(null));
        }

        [Fact]
        public void ICCBasedEqualsWhenBothMetadataAreNull()
        {
            var cs1 = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance, new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, metadata: null);
            var cs2 = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance, new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, metadata: null);

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs2, cs1);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
        }

        [Fact]
        public void ICCBasedEqualsWhenBothMetadataAreSet()
        {
            var cs1 = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance, new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, CreateTestMetadata());
            var cs2 = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance, new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, CreateTestMetadata());

            Assert.Equal(cs1, cs2);
            Assert.Equal(cs2, cs1);
            Assert.Equal(cs1.GetHashCode(), cs2.GetHashCode());
        }

        [Fact]
        public void ICCBasedMetadataComparisonIsSymmetric()
        {
            var withMetadata = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance, new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, CreateTestMetadata());
            var withoutMetadata = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance, new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, null);

            Assert.False(withMetadata.Equals(withoutMetadata));
            Assert.False(withoutMetadata.Equals(withMetadata));
        }

        private static XmpMetadata CreateTestMetadata()
        {
            var stream = new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>()), new byte[] { 1, 2, 3 });
            return new XmpMetadata(stream, TestFilterProvider.Instance, new TestPdfTokenScanner());
        }

        private static PdfFunction CreateTestFunction(double n = 1.0)
        {
            var dict = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(2) },
                { NameToken.Domain, new ArrayToken(new[] { new NumericToken(0), new NumericToken(1) }) },
                { NameToken.C0, new ArrayToken(new[] { new NumericToken(0) }) },
                { NameToken.C1, new ArrayToken(new[] { new NumericToken(1) }) },
                { NameToken.N, new NumericToken(n) }
            });
            return new PdfFunctionType2(dict,
                new ArrayToken(new[] { new NumericToken(0), new NumericToken(1) }),
                null,
                new ArrayToken(new[] { new NumericToken(0) }),
                new ArrayToken(new[] { new NumericToken(1) }),
                n);
        }
    }
}
