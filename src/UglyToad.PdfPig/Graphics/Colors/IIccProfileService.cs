namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Resolves raw ICC profile bytes into a reusable
    /// <see cref="IIccTransform"/> handle. Implementations should cache
    /// parsed profiles (recommended key: profile content hash plus intent
    /// plus component count) so the same profile is parsed at most once.
    /// </summary>
    /// <remarks>
    /// When <see cref="ParsingOptions.IccProfileService"/> is <c>null</c>
    /// or this method returns <c>false</c>, <see cref="ICCBasedColorSpaceDetails"/>
    /// falls back silently to its declared <see cref="ICCBasedColorSpaceDetails.AlternateColorSpace"/>.
    /// </remarks>
    public interface IIccProfileService
    {
        /// <summary>
        /// Try to build a transform for the given profile bytes.
        /// Returning <c>false</c> means the profile is unsupported by this
        /// backend; the caller will fall back to the alternate color space.
        /// </summary>
        bool TryGetTransform(
            Memory<byte> profileBytes,
            int numberOfColorComponents,
            RenderingIntent intent,
            [NotNullWhen(true)] out IIccTransform? transform);
    }
}
