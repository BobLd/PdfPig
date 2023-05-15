namespace UglyToad.PdfPig.Parser
{
    using System.Collections.Generic;
    using Core;
    using Graphics.Operations;
    using Logging;

    /// <summary>
    /// TODO
    /// </summary>
    public interface IPageContentParser
    {
        /// <summary>
        /// TODO
        /// </summary>
        IReadOnlyList<IGraphicsStateOperation> Parse(int pageNumber, IInputBytes inputBytes,
            ILog log);
    }
}