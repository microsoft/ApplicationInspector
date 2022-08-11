// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.IO;
using System.Reflection;
using System.Text.Json;
using JsonCons.JsonPath;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.XPath;
    /// <summary>
    ///     Class to handle text as a searchable container
    /// </summary>
    public class TextContainer
    {
        /// <summary>
        ///     Creates new instance
        /// </summary>
        /// <param name="content"> Text to work with </param>
        /// <param name="language"> The language of the test </param>
        /// <param name="languages">An instance of the <see cref="Languages"/> class containing the information for language mapping to use.</param>
        public TextContainer(string content, string language, Languages languages, ILoggerFactory? loggerFactory = null)
        {
            _logger = loggerFactory?.CreateLogger<TextContainer>() ?? NullLogger<TextContainer>.Instance;
            Language = language;
            FullContent = content;
            LineEnds = new List<int>() { 0 };
            LineStarts = new List<int>() { 0, 0 };

            // Find line end in the text
            int pos = FullContent.IndexOf('\n');
            while (pos > -1)
            {
                LineEnds.Add(pos);

                if (pos + 1 < FullContent.Length)
                {
                    LineStarts.Add(pos + 1);
                }

                pos = FullContent.IndexOf('\n', pos + 1);
            }

            if (LineEnds.Count < LineStarts.Count)
            {
                LineEnds.Add(FullContent.Length - 1);
            }

            prefix = languages.GetCommentPrefix(Language);
            suffix = languages.GetCommentSuffix(Language);
            inline = languages.GetCommentInline(Language);
            Languages = languages;
        }

        public Languages Languages { get; set; }

        private bool _triedToConstructJsonDocument;
        private JsonDocument? _jsonDocument;
        internal IEnumerable<(string, Boundary)> GetStringFromJsonPath(string Path)
        {
            if (!_triedToConstructJsonDocument)
            {
                try
                {
                    _triedToConstructJsonDocument = true;
                    _jsonDocument = JsonDocument.Parse(FullContent);
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to parse as a JSON document: {0}", e.Message);
                    _jsonDocument = null;
                }
            }

            if (_jsonDocument is not null)
            {
                var selector = JsonSelector.Parse(Path);
                
                IList<JsonElement> values = selector.Select(_jsonDocument.RootElement);

                var field = typeof(JsonElement).GetField("_idx", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field is null)
                {
                    _logger.LogWarning("Failed to access _idx field of JsonElement.");
                }
                else
                {
                    foreach (JsonElement ele in values)
                    {
                        // Private access hack
                        // The idx field is the start of the JSON element, including markup that isn't directly part of the element itself
                        if (field.GetValue(ele) is int idx)
                        {
                            var eleString = ele.ToString();
                            if (eleString is { } denulledString)
                            {
                                var location = new Boundary()
                                {
                                    // Adjust the index to the start of the actual element
                                    Index = FullContent[idx..].IndexOf(denulledString, StringComparison.Ordinal) + idx,
                                    Length = eleString.Length
                                };
                                yield return (eleString, location);
                            }
                        }
                    }   
                }
            }
        }

        private bool _triedToConstructXPathDocument;
        private XPathDocument? _xmlDoc;
        
        /// <summary>
        /// If this file is a JSON, XML or YML file, returns the string contents of the specified path.
        /// If the path does not exist, or the file is not JSON, XML or YML returns null.
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        internal IEnumerable<(string, Boundary)> GetStringFromXPath(string Path)
        {
            if (!_triedToConstructXPathDocument)
            {
                try
                {
                    _triedToConstructXPathDocument = true;
                    _xmlDoc = new XPathDocument(new StringReader(FullContent));
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to parse as an XML document: {0}", e.Message);
                    _xmlDoc = null;
                }
            }

            if (_xmlDoc is not null)
            {
                var navigator = _xmlDoc.CreateNavigator();
                var nodeIter = navigator.Select(Path);
                int minIndex = 0;
                while (nodeIter.MoveNext())
                {
                    if (nodeIter.Current is not null)
                    {
                        // First we find the name
                        var nameIndex = FullContent[minIndex..].IndexOf(nodeIter.Current.Name);
                        // Then we grab the index of the end of this tag.
                        // We can't use OuterXML because the parser will inject the namespace if present into the OuterXML so it doesn't match the original text.
                        var endTagIndex = FullContent[nameIndex..].IndexOf('>');
                        var totalOffset = nameIndex + endTagIndex + minIndex;
                        var offset = FullContent[totalOffset..].IndexOf(nodeIter.Current.InnerXml) + totalOffset;
                        // Move the minimum index up in case there are multiple instances of identical OuterXML
                        // This ensures we won't re-find the same one
                        minIndex = offset;
                        var location = new Boundary()
                        {
                            Index = offset,
                            Length = nodeIter.Current.InnerXml.Length
                        };
                        yield return (nodeIter.Current.Value, location);
                    }
                }
            }
        }
        
        /// <summary>
        /// The full string of the TextContainer represents.
        /// </summary>
        public string FullContent { get; }
        /// <summary>
        /// The code language of the file
        /// </summary>
        public string Language { get; }
        /// <summary>
        /// One-indexed array of the character indexes of the ends of the lines in FullContent.
        /// </summary>
        public List<int> LineEnds { get; }
        /// <summary>
        /// One-indexed array of the character indexes of the starts of the lines in FullContent.
        /// </summary>
        public List<int> LineStarts { get; }

        /// <summary>
        /// A dictionary mapping character index in FullContent to if a specific character is commented.  See IsCommented to use.
        /// </summary>
        private ConcurrentDictionary<int,bool> CommentedStates { get; } = new();

        /// <summary>
        /// Populates the CommentedStates Dictionary based on the index and the provided comment prefix and suffix
        /// </summary>
        /// <param name="index">The character index in FullContent</param>
        /// <param name="prefix">The comment prefix</param>
        /// <param name="suffix">The comment suffix</param>
        private void PopulateCommentedStatesInternal(int index, string prefix, string suffix)
        {
            var prefixLoc = FullContent.LastIndexOf(prefix, index, StringComparison.Ordinal);
            if (prefixLoc != -1)
            {
                if (!CommentedStates.ContainsKey(prefixLoc))
                {
                    var suffixLoc = FullContent.IndexOf(suffix, prefixLoc, StringComparison.Ordinal);
                    if (suffixLoc == -1)
                    {
                        suffixLoc = FullContent.Length - 1;
                    }
                    for (int i = prefixLoc; i <= suffixLoc; i++)
                    {
                        CommentedStates[i] = true;
                    }
                }
            }
        }

        /// <summary>
        /// Populate the CommentedStates Dictionary based on the provided index.
        /// </summary>
        /// <param name="index">The character index in FullContent to work based on.</param>
        public void PopulateCommentedState(int index)
        {
            var inIndex = index;
            if (index >= FullContent.Length)
            {
                index = FullContent.Length - 1;
            }
            if (index < 0)
            {
                index = 0;
            }
            // Populate true for the indexes of the most immediately preceding instance of the multiline comment type if found
            if (!string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(suffix))
            {
                PopulateCommentedStatesInternal(index, prefix, suffix);
            }
            // Populate true for indexes of the most immediately preceding instance of the single-line comment type if found
            if (!CommentedStates.ContainsKey(index) && !string.IsNullOrEmpty(inline))
            {
                PopulateCommentedStatesInternal(index, inline, "\n");
            }
            var i = index;
            // Everything preceding this, including this, which doesn't have a commented state is
            // therefore not commented so we backfill
            while (!CommentedStates.ContainsKey(i) && i >= 0)
            {
                CommentedStates[i--] = false;
            }
            if (inIndex != index)
            {
                CommentedStates[inIndex] = CommentedStates[index];
            }
        }

        /// <summary>
        /// Gets the text for a given boundary
        /// </summary>
        /// <param name="boundary">The boundary to get text for.</param>
        /// <returns></returns>
        public string GetBoundaryText(Boundary boundary)
        {
            if (boundary is null)
            {
                return string.Empty;
            }
            var start = Math.Max(boundary.Index, 0);
            var end = start + boundary.Length;
            start = Math.Min(FullContent.Length, start);
            end = Math.Min(FullContent.Length, end);
            return FullContent[start..end];
        }

        /// <summary>
        ///     Returns boundary for a given index in text
        /// </summary>
        /// <param name="index"> Position in text </param>
        /// <returns> Boundary </returns>
        public Boundary GetLineBoundary(int index)
        {
            Boundary result = new();

            for (int i = 0; i < LineEnds.Count; i++)
            {
                if (LineEnds[i] >= index)
                {
                    result.Index = LineStarts[i];
                    result.Length = LineEnds[i] - LineStarts[i] + 1;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        ///     Return content of the line
        /// </summary>
        /// <param name="line"> Line number (one-indexed) </param>
        /// <returns> Text </returns>
        public string GetLineContent(int line)
        {
            if (line >= LineEnds.Count)
            {
                line = LineEnds.Count - 1;
            }
            int index = LineEnds[line];
            Boundary bound = GetLineBoundary(index);
            return FullContent.Substring(bound.Index, bound.Length);
        }

        /// <summary>
        ///     Returns location (Line, Column) for given index in text
        /// </summary>
        /// <param name="index"> Position in text (line is one-indexed)</param>
        /// <returns> Location </returns>
        public Location GetLocation(int index)
        {
            for (int i = 1; i < LineEnds.Count; i++)
            {
                if (LineEnds[i] >= index)
                {
                    return new()
                    {
                        Column = index - LineStarts[i],
                        Line = i
                    };
                }
            }

            return new();
        }

        public bool IsCommented(int index)
        {
            if (!CommentedStates.ContainsKey(index))
            {
                PopulateCommentedState(index);
            }
            return CommentedStates[index];
        }

        /// <summary>
        ///     Check whether the boundary in a text matches the scope of a search pattern (code, comment etc.)
        /// </summary>
        /// <param name="pattern"> The scopes to check </param>
        /// <param name="boundary"> Boundary in the text </param>
        /// <param name="text"> Text </param>
        /// <returns> True if boundary is in a provided scope </returns>
        public bool ScopeMatch(IEnumerable<PatternScope> scopes, Boundary boundary)
        {
            if (scopes is null || !scopes.Any() || scopes.Contains(PatternScope.All))
            {
                return true;
            }
            if (scopes.Contains(PatternScope.All) || string.IsNullOrEmpty(prefix))
                return true;
            bool isInComment = IsCommented(boundary.Index);

            return (!isInComment && scopes.Contains(PatternScope.Code)) || (isInComment && scopes.Contains(PatternScope.Comment));
        }

        private readonly string inline;
        private readonly string prefix;
        private readonly string suffix;
        private readonly ILogger _logger;
    }
}