namespace UglyToad.PdfPig.Tokens
{
    /// <summary>
    /// Inline image data is used to embed images in PDF content streams. The content is wrapped by ID and ED tags in a BI operation.
    /// </summary>
    public class InlineImageDataToken : IDataToken<byte[]>
    {
        /// <inheritdoc />
        public byte[] Data { get; }

        /// <summary>
        /// Create a new <see cref="InlineImageDataToken"/>.
        /// </summary>
        /// <param name="data"></param>
        public InlineImageDataToken(byte[] data)
        {
            Data = data;
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is InlineImageDataToken other))
            {
                return false;
            }

            if (Data.Length != other.Data.Length)
            {
                return false;
            }

            for (var index = 0; index < Data.Length; ++index)
            {
                if (Data[index] != other.Data[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}