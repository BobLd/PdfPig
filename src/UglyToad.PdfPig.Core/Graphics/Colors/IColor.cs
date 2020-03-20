﻿namespace UglyToad.PdfPig.Core.Graphics.Colors
{
    /// <summary>
    /// A color used for text or paths in a PDF.
    /// </summary>
    public interface IColor
    {
        /// <summary>
        /// The colorspace used for this color.
        /// </summary>
        ColorSpace ColorSpace { get; }

        /// <summary>
        /// The color as RGB values (between 0 and 1).
        /// </summary>
        (decimal r, decimal g, decimal b) ToRGBValues();
    }
}
