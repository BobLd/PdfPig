namespace UglyToad.PdfPig.Content
{
    using System;

    internal class HeaderVersion
    {
        public double Version { get; }

        public string VersionString { get; }

        /// <summary>
        /// The offset in bytes from the start of the file to the start of the version comment.
        /// </summary>
        public int OffsetInFile { get; }

        public HeaderVersion(double version, string versionString, int offsetInFile)
        {
            Version = version;
            VersionString = versionString;
            if (offsetInFile < 0)
            {
                throw new ArgumentOutOfRangeException($"Invalid offset for header version, must be positive. Got: {offsetInFile}.");
            }

            OffsetInFile = offsetInFile;
        }

        public override string ToString()
        {
            return $"Version: {VersionString}";
        }
    }
}