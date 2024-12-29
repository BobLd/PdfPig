namespace UglyToad.PdfPig.Core
{
    /// <summary>
    /// This class will be used to signify a range. a(min) &lt;= a* &lt;= a(max)
    /// </summary>
    public readonly struct PdfRange
    {
        private readonly double[] rangeArray;
        private readonly int startingIndex;

        /// <summary>
        /// Constructor with an index into an array. Because some arrays specify
        /// multiple ranges ie [0, 1, 0, 2, 2, 3]. It is convenient for this
        /// class to take an index into an array. So if you want this range to
        /// represent 0, 2 in the above example then you would say <c>new PDRange(array, 1)</c>.
        /// </summary>
        /// <param name="range">The array that describes the index</param>
        /// <param name="index">The range index into the array for the start of the range.</param>
        public PdfRange(double[] range, int index)
        {
            rangeArray = range;
            startingIndex = index;
        }

        /// <summary>
        /// The minimum value of the range.
        /// </summary>
        public double Min => rangeArray[startingIndex * 2];

        /// <summary>
        /// The maximum value of the range.
        /// </summary>
        public double Max => rangeArray[startingIndex * 2 + 1];
    }
}
