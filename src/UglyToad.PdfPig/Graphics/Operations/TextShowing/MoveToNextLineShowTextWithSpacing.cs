namespace UglyToad.PdfPig.Graphics.Operations.TextShowing
{
    using System.IO;
    using TextPositioning;
    using TextState;

    /// <inheritdoc />
    /// <summary>
    /// Move to the next line and show a text string, using the first number as the word spacing and the second as the character spacing.
    /// </summary>
    public class MoveToNextLineShowTextWithSpacing : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "\"";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The word spacing.
        /// </summary>
        public double WordSpacing { get; }

        /// <summary>
        /// The character spacing.
        /// </summary>
        public double CharacterSpacing { get; }

        /// <summary>
        /// The bytes of the text.
        /// </summary>
        public ReadOnlyMemory<byte> Bytes { get; }

        /// <summary>
        /// The text to show.
        /// </summary>
        public string? Text { get; }

        /// <summary>
        /// Create a new <see cref="MoveToNextLineShowTextWithSpacing"/>.
        /// </summary>
        /// <param name="wordSpacing">The word spacing.</param>
        /// <param name="characterSpacing">The character spacing.</param>
        /// <param name="text">The text to show.</param>
        public MoveToNextLineShowTextWithSpacing(double wordSpacing, double characterSpacing, string text)
        {
            WordSpacing = wordSpacing;
            CharacterSpacing = characterSpacing;
            Text = text;
            showText = new ShowText(text);
        }

        /// <summary>
        /// Create a new <see cref="MoveToNextLineShowTextWithSpacing"/>.
        /// </summary>
        /// <param name="wordSpacing">The word spacing.</param>
        /// <param name="characterSpacing">The character spacing.</param>
        /// <param name="hexBytes">The bytes of the text to show.</param>
        public MoveToNextLineShowTextWithSpacing(double wordSpacing, double characterSpacing, ReadOnlyMemory<byte> hexBytes)
        {
            WordSpacing = wordSpacing;
            CharacterSpacing = characterSpacing;
            Bytes = hexBytes;
            showText = new ShowText(hexBytes);
        }

        /// <summary>
        /// The show text operation this one ends with, built once rather than per run.
        /// </summary>
        private readonly ShowText showText;

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            new SetWordSpacing(WordSpacing).Run(operationContext);
            new SetCharacterSpacing(CharacterSpacing).Run(operationContext);
            MoveToNextLine.Value.Run(operationContext);
            showText.Run(operationContext);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDouble(WordSpacing);
            stream.WriteWhiteSpace();
            stream.WriteDouble(CharacterSpacing);
            stream.WriteWhiteSpace();

            if (!Bytes.IsEmpty)
            {
                stream.WriteHex(Bytes.Span);
            }
            else
            {
                stream.WriteText($"({Text})");
            }

            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{WordSpacing} {CharacterSpacing} {Text} {Symbol}";
        }
    }
}