namespace UglyToad.PdfPig.Graphics
{
    using System.Collections.Generic;
    using Operations;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// TODO
    /// </summary>
    public interface IGraphicsStateOperationFactory
    {
        /// <summary>
        /// TODO
        /// </summary>
        [CanBeNull]
        IGraphicsStateOperation Create(OperatorToken op, IReadOnlyList<IToken> operands);
    }
}