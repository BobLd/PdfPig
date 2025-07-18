﻿namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Graphics;
    using Graphics.Operations;
    using Graphics.Operations.InlineImages;
    using Graphics.Operations.TextObjects;
    using Logging;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Provides functionality to parse the content of a PDF page, extracting graphics state operations
    /// from the input data. This class is responsible for interpreting the PDF content stream and
    /// converting it into a collection of operations.
    /// </summary>
    public sealed class PageContentParser : IPageContentParser
    {
        private readonly IGraphicsStateOperationFactory operationFactory;
        private readonly bool useLenientParsing;

        /// <summary>
        /// Initialises a new instance of the <see cref="PageContentParser"/> class.
        /// </summary>
        /// <param name="operationFactory">
        /// The factory responsible for creating graphics state operations.
        /// </param>
        /// <param name="useLenientParsing">
        /// A value indicating whether lenient parsing should be used. Defaults to <c>false</c>.
        /// </param>
        public PageContentParser(IGraphicsStateOperationFactory operationFactory, bool useLenientParsing = false)
        {
            this.operationFactory = operationFactory;
            this.useLenientParsing = useLenientParsing;
        }

        /// <summary>
        /// Parses the content of a PDF page and extracts a collection of graphics state operations.
        /// </summary>
        /// <param name="pageNumber">The number of the page being parsed.</param>
        /// <param name="inputBytes">The input bytes representing the content of the page.</param>
        /// <param name="log">The logger instance for recording parsing-related information.</param>
        /// <returns>A read-only list of graphics state operations extracted from the page content.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="inputBytes"/> or <paramref name="log"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the parsing process encounters an invalid or unsupported token.
        /// </exception>
        public IReadOnlyList<IGraphicsStateOperation> Parse(
            int pageNumber,
            IInputBytes inputBytes,
            ILog log)
        {
            var scanner = new CoreTokenScanner(inputBytes, false, useLenientParsing: useLenientParsing);

            var precedingTokens = new List<IToken>();
            var graphicsStateOperations = new List<IGraphicsStateOperation>();

            var lastEndImageOffset = new long?();

            while (scanner.MoveNext())
            {
                var token = scanner.CurrentToken;

                if (token is InlineImageDataToken inlineImageData)
                {
                    var dictionary = new Dictionary<NameToken, IToken>();

                    for (var i = 0; i < precedingTokens.Count - 1; i++)
                    {
                        var t = precedingTokens[i];
                        if (!(t is NameToken n))
                        {
                            continue;
                        }

                        i++;

                        dictionary[n] = precedingTokens[i];
                    }

                    graphicsStateOperations.Add(new BeginInlineImageData(dictionary));
                    graphicsStateOperations.Add(new EndInlineImage(inlineImageData.Data));

                    lastEndImageOffset = scanner.CurrentPosition - 2;

                    precedingTokens.Clear();
                }
                else if (token is OperatorToken op)
                {
                    // Handle an end image where the stream of image data contained EI but was not actually a real end image operator.
                    if (op.Data == "EI")
                    {
                        // Check an end image operation was the last thing that happened.
                        IGraphicsStateOperation? lastOperation = graphicsStateOperations.Count > 0
                            ? graphicsStateOperations[graphicsStateOperations.Count - 1]
                            : null;

                        if (lastEndImageOffset is null || lastOperation is null || !(lastOperation is EndInlineImage lastEndImage))
                        {
                            throw new PdfDocumentFormatException("Encountered End Image token outside an inline image on " +
                                                                 $"page {pageNumber} at offset in content: {scanner.CurrentPosition}.");
                        }

                        // Work out how much data we missed between the false EI operator and the actual one.
                        var actualEndImageOffset = scanner.CurrentPosition - 3;

                        log.Warn($"End inline image (EI) encountered after previous EI, attempting recovery at {actualEndImageOffset}.");

                        var gap = (int)(actualEndImageOffset - lastEndImageOffset);

                        var from = inputBytes.CurrentOffset;
                        inputBytes.Seek(lastEndImageOffset.Value);

                        // Recover the full image data.
                        {
                            var missingData = new byte[gap];
                            var read = inputBytes.Read(missingData);
                            if (read != gap)
                            {
                                throw new InvalidOperationException($"Failed to read expected buffer length {gap} on page {pageNumber} " +
                                                                    $"when reading inline image at offset in content: {lastEndImageOffset.Value}.");
                            }

                            // Replace the last end image operator with one containing the full set of data.
                            graphicsStateOperations.Remove(lastEndImage);
                            graphicsStateOperations.Add(new EndInlineImage([.. lastEndImage.ImageData.Span, .. missingData.AsSpan()]));
                        }

                        lastEndImageOffset = actualEndImageOffset;

                        inputBytes.Seek(from);
                    }
                    else
                    {
                        IGraphicsStateOperation? operation;
                        try
                        {
                            operation = operationFactory.Create(op, precedingTokens);
                        }
                        catch (Exception ex)
                        {
                            // End images can cause weird state if the "EI" appears inside the inline data stream.
                            log.Error($"Failed reading operation at offset {inputBytes.CurrentOffset} for page {pageNumber}, data: '{op.Data}'", ex);
                            if (TryGetLastEndImage(graphicsStateOperations, out _, out _)
                                || useLenientParsing)
                            {
                                operation = null;
                            }
                            else
                            {
                                throw;
                            }
                        }

                        if (operation != null)
                        {
                            graphicsStateOperations.Add(operation);
                        }
                        else if (graphicsStateOperations.Count > 0)
                        {
                            if (TryGetLastEndImage(graphicsStateOperations, out var prevEndInlineImage, out var index) && lastEndImageOffset.HasValue)
                            {
                                log.Warn($"Operator {op.Data} was not understood following end of inline image data at {lastEndImageOffset}, " +
                                         "attempting recovery.");

                                var nextByteSet = scanner.RecoverFromIncorrectEndImage(lastEndImageOffset.Value);
                                graphicsStateOperations.RemoveRange(index, graphicsStateOperations.Count - index);
                                var newEndInlineImage = new EndInlineImage([.. prevEndInlineImage.ImageData.Span, .. nextByteSet]);
                                graphicsStateOperations.Add(newEndInlineImage);
                                lastEndImageOffset = scanner.CurrentPosition - 3;
                            }
                            else if (op.Data == "inf")
                            {
                                // Value representing infinity in broken file from #467.
                                // Treat as zero.
                                precedingTokens.Add(NumericToken.Zero);
                                continue;
                            }
                            else
                            {
                                log.Warn($"Operator which was not understood encountered. Values was {op.Data}. Ignoring.");
                            }
                        }
                    }

                    precedingTokens.Clear();
                }
                else if (token is CommentToken)
                {
                }
                else
                {
                    precedingTokens.Add(token);
                }
            }

            return graphicsStateOperations;
        }

        private static bool TryGetLastEndImage(List<IGraphicsStateOperation> graphicsStateOperations, [NotNullWhen(true)] out EndInlineImage? endImage, out int index)
        {
            index = -1;
            endImage = null;

            if (graphicsStateOperations.Count == 0)
            {
                return false;
            }

            for (int i = graphicsStateOperations.Count - 1; i >= 0; i--)
            {
                var last = graphicsStateOperations[i];

                if (last is EndInlineImage ei)
                {
                    endImage = ei;
                    index = i;
                    return true;
                }

                if (last is EndText or BeginInlineImageData)
                {
                    break;
                }
            }

            return false;
        }
    }
}
