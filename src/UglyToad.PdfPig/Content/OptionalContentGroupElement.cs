using UglyToad.PdfPig.Tokenization.Scanner;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Content
{
    /// <summary>
    /// An optional content group is a dictionary representing a collection of graphics
    /// that can be made visible or invisible dynamically by users of viewers applications.
    /// </summary>
    public sealed class OptionalContentGroupElement
    {
        /// <summary>
        /// Text labels in nested arrays shall be used to present collections of related optional content groups,
        /// and not to communicate actual nesting of content inside multiple layers of groups.
        /// </summary>
        public string? Label { get; }

        /// <summary>
        /// The type of PDF object that this dictionary describes.
        /// <para>Must be OCG for an optional content group dictionary.</para>
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// The name of the optional content group, suitable for presentation in a viewer application's user interface.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// A single name or an array containing any combination of names.
        /// <para>Default value is 'View'.</para>
        /// </summary>
        public IReadOnlyList<string>? Intent { get; }

        /// <summary>
        /// A usage dictionary describing the nature of the content controlled by the group.
        /// </summary>
        public IReadOnlyDictionary<string, IToken>? Usage { get; }

        /// <summary>
        /// Nested contents.
        /// </summary>
        public IReadOnlyList<OptionalContentGroupElement>? Nested => NestedRaw;
        
        internal List<OptionalContentGroupElement>? NestedRaw { get; set; }

        /// <summary>
        /// Underlying <see cref="MarkedContentElement"/>.
        /// </summary>
        public MarkedContentElement MarkedContent { get; }

        internal OptionalContentGroupElement(string? label = null)
        {
            Label = label;
        }

        internal OptionalContentGroupElement(MarkedContentElement markedContentElement, IPdfTokenScanner pdfTokenScanner)
            : this(markedContentElement.Properties, pdfTokenScanner)
        {
            MarkedContent = markedContentElement;
        }
        
        internal OptionalContentGroupElement(DictionaryToken markedContentProperties, IPdfTokenScanner pdfTokenScanner, string? label = null)
            : this(label)
        {
            // Type - Required
            if (markedContentProperties.TryGet(NameToken.Type, pdfTokenScanner, out NameToken? type))
            {
                Type = type.Data;
            }
            else if (markedContentProperties.TryGet(NameToken.Type, pdfTokenScanner, out StringToken? typeStr))
            {
                Type = typeStr.Data;
            }
            else
            {
                throw new ArgumentException($"Cannot parse optional content's {nameof(Type)} from {nameof(markedContentProperties)}. This is a required field.",
                    nameof(markedContentProperties));
            }

            switch (Type)
            {
                case "OCG": // Optional content group dictionary
                    // Name - Required
                    if (markedContentProperties.TryGet(NameToken.Name, pdfTokenScanner, out NameToken? name))
                    {
                        Name = name.Data;
                    }
                    else if (markedContentProperties.TryGet(NameToken.Name, pdfTokenScanner, out StringToken? nameStr))
                    {
                        Name = nameStr.Data;
                    }
                    else if (markedContentProperties.TryGet(NameToken.Name, pdfTokenScanner, out HexToken? nameHex))
                    {
                        Name = nameHex.Data;
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot parse optional content's {nameof(Name)} from {nameof(markedContentProperties)}. This is a required field.",
                            nameof(markedContentProperties));
                    }

                    // Intent - Optional
                    if (markedContentProperties.TryGet(NameToken.Intent, pdfTokenScanner, out NameToken? intentName))
                    {
                        Intent = [intentName.Data];
                    }
                    else if (markedContentProperties.TryGet(NameToken.Intent, pdfTokenScanner, out StringToken? intentStr))
                    {
                        Intent = [intentStr.Data];
                    }
                    else if (markedContentProperties.TryGet(NameToken.Intent, pdfTokenScanner, out ArrayToken? intentArray))
                    {
                        List<string> intentList = new List<string>();
                        foreach (var token in intentArray.Data)
                        {
                            if (token is NameToken nameA)
                            {
                                intentList.Add(nameA.Data);
                            }
                            else if (token is StringToken strA)
                            {
                                intentList.Add(strA.Data);
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }

                        Intent = intentList;
                    }
                    else
                    {
                        // Default value is 'View'.
                        Intent = ["View"];
                    }

                    // Usage - Optional
                    if (markedContentProperties.TryGet(NameToken.Usage, pdfTokenScanner, out DictionaryToken? usage))
                    {
                        Usage = usage.Data;
                    }

                    break;

                case "OCMD":
                    // OCGs - Optional
                    if (markedContentProperties.TryGet(NameToken.Ocgs, pdfTokenScanner, out DictionaryToken? ocgsD))
                    {
                        // dictionary or array
                        throw new NotImplementedException($"{NameToken.Ocgs}");
                    }
                    else if (markedContentProperties.TryGet(NameToken.Ocgs, pdfTokenScanner, out ArrayToken? ocgsA))
                    {
                        // dictionary or array
                        throw new NotImplementedException($"{NameToken.Ocgs}");
                    }

                    // P - Optional
                    if (markedContentProperties.TryGet(NameToken.P, pdfTokenScanner, out NameToken? p))
                    {
                        throw new NotImplementedException($"{NameToken.P}");
                    }

                    // VE - Optional
                    if (markedContentProperties.TryGet(NameToken.VE, pdfTokenScanner, out ArrayToken? ve))
                    {
                        throw new NotImplementedException($"{NameToken.VE}");
                    }

                    break;

                default:
                    throw new ArgumentException($"Unknown Optional Content of type '{Type}' not known.", nameof(Type));
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override string ToString()
        {
            return $"{Type} - {Name} [{string.Join(",", Intent)}]: {MarkedContent?.ToString()}";
        }
    }
}
