﻿namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Runtime.Serialization;

    /// <inheritdoc />
    /// <summary>
    /// This exception will be thrown where the contents of the PDF document do not match the specification in such a way that it
    /// renders the document unreadable.
    /// </summary>
    [Serializable]
    public class PdfDocumentFormatException : Exception
    {
        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="T:UglyToad.PdfPig.Exceptions.PdfDocumentFormatException" />.
        /// </summary>
        public PdfDocumentFormatException()
        {
        }

        /// <inheritdoc />
        public PdfDocumentFormatException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public PdfDocumentFormatException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
