namespace UglyToad.PdfPig.Graphics.Colors.DeviceN
{
    using Tokens;

    // https://github.com/apache/pdfbox/blob/trunk/pdfbox/src/main/java/org/apache/pdfbox/pdmodel/graphics/color/PDDeviceNProcess.java#L57
    public sealed class DeviceNProcess
    {
        //public DictionaryToken Dictionary { get; }

        public ColorSpaceDetails? ColorSpace { get; }

        /// <summary>
        /// The names of the color components.
        /// </summary>
        public NameToken[] Components { get; }

        public DeviceNProcess(ColorSpaceDetails? colorSpace, NameToken[]? components)
        {
            ColorSpace = colorSpace;
            Components = components ?? [];
        }
    }
}
