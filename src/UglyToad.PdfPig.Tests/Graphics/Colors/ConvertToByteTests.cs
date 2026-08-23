namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using System;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Core;
    using Xunit;

    /// <summary>
    /// <see cref="ColorSpaceDetails.ConvertToByte"/> is the last line of defence for every colour space:
    /// a component that arrives outside <c>[0, 1]</c> must be clipped rather than cast, because casting a
    /// <see cref="double"/> outside <see cref="byte"/>'s range is undefined in C#.
    /// </summary>
    public class ConvertToByteTests
    {
        /// <summary>
        /// <see cref="ColorSpaceDetails.ConvertToByte"/> is protected, so reaching it means deriving.
        /// </summary>
        private sealed class ByteConverter : ColorSpaceDetails
        {
            public ByteConverter() : base(ColorSpace.DeviceGray)
            {
            }

            public static byte Convert(double value) => ConvertToByte(value);

            public override int NumberOfColorComponents => 1;

            public override int BaseNumberOfColorComponents => 1;

            public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
                => throw new NotSupportedException();

            public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
                out double r, out double g, out double b) => throw new NotSupportedException();

            public override IColor? GetInitializeColor(RenderingIntent intent) => throw new NotSupportedException();

            internal override double[] Process(double[] values, RenderingIntent intent) => throw new NotSupportedException();

            internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent) => throw new NotSupportedException();
        }

        [Theory]
        [InlineData(0.0, 0)]
        [InlineData(1.0, 255)]
        [InlineData(0.5, 128)]      // 127.5 rounds away from zero, as it always did
        [InlineData(0.25, 64)]      // 63.75
        [InlineData(0.001, 0)]      // 0.255
        [InlineData(0.999, 255)]    // 254.745
        public void ConvertToByteMatchesTheRoundingItAlwaysHad(double value, byte expected)
        {
            Assert.Equal(expected, ByteConverter.Convert(value));
        }

        [Theory]
        [InlineData(-0.0001)]
        [InlineData(-1.0)]
        [InlineData(double.NaN)]
        [InlineData(double.NegativeInfinity)]
        public void ConvertToByteFloorsAnythingBelowTheRange(double value)
        {
            Assert.Equal(0, ByteConverter.Convert(value));
        }

        [Theory]
        [InlineData(1.0001)]
        [InlineData(2.0)]
        [InlineData(double.PositiveInfinity)]
        public void ConvertToByteCapsAnythingAboveTheRange(double value)
        {
            Assert.Equal(255, ByteConverter.Convert(value));
        }
    }
}
