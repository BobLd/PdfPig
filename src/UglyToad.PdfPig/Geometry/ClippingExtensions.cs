namespace UglyToad.PdfPig.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ClipperLibrary;
    using Core;
    using Graphics;
    using Logging;
    using static Core.PdfSubpath;

    /// <summary>
    /// Applies clipping from a clipping path to another path.
    /// </summary>
    internal static class ClippingExtensions
    {
        public const double Factor = 10_000.0;

        /// <summary>
        /// Number of lines to use when transforming bezier curve to polyline.
        /// </summary>
        private const int LinesInCurve = 10;

        /// <summary>
        /// Generates the result of applying a clipping path to another path.
        /// </summary>
        public static PdfPath? Clip(this PdfPath clipping, PdfPath subject, ILog? log = null)
        {
            if (clipping is null)
            {
                throw new ArgumentNullException(nameof(clipping), $"{nameof(Clip)}: the clipping path cannot be null.");
            }

            if (!clipping.IsClipping)
            {
                throw new ArgumentException($"{nameof(Clip)}: the clipping path does not have the IsClipping flag set to true.", nameof(clipping));
            }

            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject), $"{nameof(Clip)}: the subject path cannot be null.");
            }

            if (subject.Count == 0)
            {
                return subject;
            }

            var clipper = new Clipper();

            // Clipping path
            foreach (var subPathClipping in clipping)
            {
                if (subPathClipping.Commands.Count == 0)
                {
                    continue;
                }

                // Force close clipping polygon
                if (!subPathClipping.IsClosed())
                {
                    subPathClipping.CloseSubpath();
                }

                if (!clipper.AddPath(subPathClipping.ToClipperPolygon(), ClipperPolyType.Clip, true))
                {
                    log?.Error("ClippingExtensions.Clip(): failed to add clipping subpath.");
                }
            }

            // Subject path
            // Filled and clipping path need to be closed
            bool subjectClose = subject.IsFilled || subject.IsClipping;
            foreach (var subPathSubject in subject)
            {
                if (subPathSubject.Commands.Count == 0)
                {
                    continue;
                }

                if (subjectClose && !subPathSubject.IsClosed()
                    && subPathSubject.Commands.Count(sp => sp is Line) < 2
                    && subPathSubject.Commands.Count(sp => sp is BezierCurve) == 0)
                {
                    // strange here:
                    // the subpath contains maximum 1 line and no curves
                    // it cannot be filled or a be clipping path
                    // cancel closing the path/subpath
                    subjectClose = false;
                }

                // Force close subject if need be
                if (subjectClose && !subPathSubject.IsClosed())
                {
                    subPathSubject.CloseSubpath();
                }

                if (!clipper.AddPath(subPathSubject.ToClipperPolygon(), ClipperPolyType.Subject, subjectClose))
                {
                    log?.Error("ClippingExtensions.Clip(): failed to add subject subpath for clipping.");
                }
            }

            var clippingFillType = clipping.FillingRule == FillingRule.NonZeroWinding ? ClipperPolyFillType.NonZero : ClipperPolyFillType.EvenOdd;
            var subjectFillType = subject.FillingRule == FillingRule.NonZeroWinding ? ClipperPolyFillType.NonZero : ClipperPolyFillType.EvenOdd;

            if (!subjectClose)
            {
                PdfPath clippedPath = subject.CloneEmpty();

                // Case where subject is not closed
                var solutions = new ClipperPolyTree();
                if (clipper.Execute(ClipperClipType.Intersection, solutions, subjectFillType, clippingFillType))
                {
                    foreach (var solution in solutions.Children)
                    {
                        if (solution.Contour.Count > 0)
                        {
                            PdfSubpath clippedSubpath = new PdfSubpath();
                            clippedSubpath.MoveTo(solution.Contour[0].X / Factor, solution.Contour[0].Y / Factor);

                            for (int i = 1; i < solution.Contour.Count; i++)
                            {
                                clippedSubpath.LineTo(solution.Contour[i].X / Factor, solution.Contour[i].Y / Factor);
                            }
                            clippedPath.Add(clippedSubpath);
                        }
                    }

                    if (clippedPath.Count > 0)
                    {
                        return clippedPath;
                    }
                }

                return null;
            }
            else
            {
                PdfPath clippedPath = subject.CloneEmpty();

                // Case where subject is closed
                var solutions = new List<List<ClipperIntPoint>>();
                if (!clipper.Execute(ClipperClipType.Intersection, solutions, subjectFillType, clippingFillType))
                {
                    return null;
                }

                foreach (var solution in solutions)
                {
                    if (solution.Count > 0)
                    {
                        PdfSubpath clippedSubpath = new PdfSubpath();
                        clippedSubpath.MoveTo(solution[0].X / Factor, solution[0].Y / Factor);

                        for (int i = 1; i < solution.Count; i++)
                        {
                            clippedSubpath.LineTo(solution[i].X / Factor, solution[i].Y / Factor);
                        }
                        clippedSubpath.CloseSubpath();
                        clippedPath.Add(clippedSubpath);
                    }
                }

                if (clippedPath.Count > 0)
                {
                    return clippedPath;
                }

                return null;
            }
        }

        /// <summary>
        /// Converts a path to a set of points for the Clipper algorithm to use.
        /// Allows duplicate points as they will be removed by Clipper.
        /// </summary>
        internal static List<ClipperIntPoint> ToClipperPolygon(this PdfSubpath pdfPath)
        {
            if (pdfPath.Commands.Count == 0)
            {
                return new List<ClipperIntPoint>();
            }

            var result = new List<ClipperIntPoint>();
            ClipperIntPoint movePoint;
            if (pdfPath.Commands[0] is Move move)
            {
                movePoint = move.Location.ToClipperIntPoint();
                result.Add(movePoint);

                if (pdfPath.Commands.Count == 1)
                {
                    return result;
                }
            }
            else
            {
                throw new ArgumentException($"ToClipperPolygon(): First command is not a Move command. Type is '{pdfPath.Commands[0].GetType()}'.", nameof(pdfPath));
            }

            for (var i = 1; i < pdfPath.Commands.Count; i++)
            {
                var command = pdfPath.Commands[i];
                if (command is Move)
                {
                    throw new ArgumentException("ToClipperPolygon(): only one move allowed per subpath.", nameof(pdfPath));
                }

                if (command is Line line)
                {
                    result.Add(line.From.ToClipperIntPoint());
                    result.Add(line.To.ToClipperIntPoint());
                }
                else if (command is BezierCurve curve)
                {
                    foreach (var lineB in curve.ToLines(LinesInCurve))
                    {
                        result.Add(lineB.From.ToClipperIntPoint());
                        result.Add(lineB.To.ToClipperIntPoint());
                    }
                }
                else if (command is Close)
                {
                    result.Add(movePoint);
                }
            }

            return result;
        }

        internal static List<ClipperIntPoint> ToClipperPolygon(this PdfRectangle rectangle)
        {
            return new List<ClipperIntPoint>(4)
            {
                rectangle.BottomLeft.ToClipperIntPoint(),
                rectangle.TopLeft.ToClipperIntPoint(),
                rectangle.TopRight.ToClipperIntPoint(),
                rectangle.BottomRight.ToClipperIntPoint()
            };
        }

        internal static ClipperIntPoint ToClipperIntPoint(this PdfPoint point)
        {
            return new ClipperIntPoint(point.X * Factor, point.Y * Factor);
        }

        internal static List<ClipperIntPoint> ToClipperIntPoint(this PdfLine line)
        {
            return new List<ClipperIntPoint>() { line.Point1.ToClipperIntPoint(), line.Point2.ToClipperIntPoint() };
        }
    }
}
