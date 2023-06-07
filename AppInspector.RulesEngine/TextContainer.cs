// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml;
using System.Xml.XPath;
using gfs.YamlDotNet.YamlPath;
using JsonCons.JsonPath;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YamlDotNet.RepresentationModel;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     Class to handle text as a searchable container
/// </summary>
public class TextContainer
{
    private readonly ILogger _logger;

    private readonly string inline;
    private readonly string prefix;
    private readonly string suffix;

    private bool _triedToConstructJsonDocument;
    private JsonDocument? _jsonDocument;
    private object _jsonLock = new();

    private bool _triedToConstructXPathDocument;
    private XPathDocument? _xmlDoc;
    private object _xpathLock = new();

    private bool _triedToConstructYmlDocument;
    private YamlStream? _ymlDocument;
    private object _yamlLock = new();
    private readonly string? _filePath;

    /// <summary>
    ///     Creates new instance
    /// </summary>
    /// <param name="content"> Text to work with </param>
    /// <param name="language"> The language of the test </param>
    /// <param name="languages">
    ///     An instance of the <see cref="Languages" /> class containing the information for language
    ///     mapping to use.
    /// </param>
    /// <param name="loggerFactory">An optional logger factory to receive error messages</param>
    /// <param name="filePath">A string which represents the location of the file the contents of this container contains used to enrich error messages to the <paramref name="loggerFactory"/></param>
    public TextContainer(string content, string language, Languages languages, ILoggerFactory? loggerFactory = null, string? filePath = null)
    {
        _filePath = filePath;
        _logger = loggerFactory?.CreateLogger<TextContainer>() ?? NullLogger<TextContainer>.Instance;
        Language = language;
        FullContent = content;
        LineEnds = new List<int> { 0 };
        LineStarts = new List<int> { 0, 0 };

        // Find line end in the text
        var pos = FullContent.IndexOf('\n');
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

    /// <summary>
    ///     The full string of the TextContainer represents.
    /// </summary>
    public string FullContent { get; }

    /// <summary>
    ///     The code language of the file
    /// </summary>
    public string Language { get; }

    /// <summary>
    ///     One-indexed array of the character indexes of the ends of the lines in FullContent.
    /// </summary>
    public List<int> LineEnds { get; }

    /// <summary>
    ///     One-indexed array of the character indexes of the starts of the lines in FullContent.
    /// </summary>
    public List<int> LineStarts { get; }

    /// <summary>
    ///     A dictionary mapping character index in FullContent to if a specific character is commented.  See IsCommented to
    ///     use.
    /// </summary>
    private ConcurrentDictionary<int, bool> CommentedStates { get; } = new();

    internal IEnumerable<(string, Boundary)> GetStringFromJsonPath(string Path)
    {
        lock (_jsonLock)
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
                    _logger.LogError("Failed to parse {1} as a JSON document: {0}", e.Message, _filePath);
                    _jsonDocument = null;
                }
            }
        }

        if (_jsonDocument is null)
        {
            yield break;
        }

        var selector = JsonSelector.Parse(Path);

        var values = selector.Select(_jsonDocument.RootElement);

        var field = typeof(JsonElement).GetField("_idx", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field is null)
        {
            _logger.LogWarning("Failed to access _idx field of JsonElement.");
        }
        else
        {
            foreach (var ele in values)
                // Private access hack
                // The idx field is the start of the JSON element, including markup that isn't directly part of the element itself
                if (field.GetValue(ele) is int idx)
                {
                    var eleString = ele.ToString();
                    if (eleString is { } denulledString)
                    {
                        var location = new Boundary
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

    /// <summary>
    ///     If this file is a JSON, XML or YML file, returns the string contents of the specified path.
    ///     If the path does not exist, or the file is not JSON, XML or YML returns null.
    /// </summary>
    /// <param name="Path"></param>
    /// <returns></returns>
    internal IEnumerable<(string, Boundary)> GetStringFromXPath(string Path, Dictionary<string, string> xpathNameSpaces)
    {
        lock (_xpathLock)
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
                    _logger.LogError("Failed to parse {1} as a XML document: {0}", e.Message, _filePath);
                    _xmlDoc = null;
                }
            }
        }

        if (_xmlDoc is null)
        {
            yield break;
        }

        var navigator = _xmlDoc.CreateNavigator();
        var query = navigator.Compile(Path);
        if (xpathNameSpaces.Any())
        {
            var manager = new XmlNamespaceManager(navigator.NameTable);
            foreach (var pair in xpathNameSpaces)
            {
                manager.AddNamespace(pair.Key, pair.Value);
            }
            query.SetContext(manager);
        }
        var nodeIter = navigator.Select(query);
        var minIndex = 0;
        while (nodeIter.MoveNext())
        {
            if (nodeIter.Current is null)
            {
                continue;
            }

            // First we find the name
            var nameIndex = FullContent[minIndex..].IndexOf(nodeIter.Current.Name, StringComparison.Ordinal) + minIndex;
            // Then we grab the index of the end of this tag.
            // We can't use OuterXML because the parser will inject the namespace if present into the OuterXML so it doesn't match the original text.
            var endTagIndex = FullContent[nameIndex..].IndexOf('>');
            // We also look for self-closing tag
            var selfClosedTag = FullContent[endTagIndex-1] == '/';
            // If the tag is self closing innerxml will be empty string, so the finding is located at the end of the tag and is empty string
            // Otherwise the finding is the content of the xml tag
            var offset = selfClosedTag ? endTagIndex : FullContent[nameIndex..].IndexOf(nodeIter.Current.InnerXml, StringComparison.Ordinal) + nameIndex;
            // Move the minimum index up in case there are multiple instances of identical OuterXML
            // This ensures we won't re-find the same one
            var totalOffset = minIndex + nameIndex + endTagIndex;
            minIndex = totalOffset;
            var location = new Boundary
            {
                Index = offset,
                Length = nodeIter.Current.InnerXml.Length
            };
            yield return (nodeIter.Current.Value, location);
        }
    }

    /// <summary>
    ///     Populates the CommentedStates Dictionary based on the index and the provided comment prefix and suffix
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

                for (var i = prefixLoc; i <= suffixLoc; i++) CommentedStates[i] = true;
            }
        }
    }

    /// <summary>
    ///     Populate the CommentedStates Dictionary based on the provided index.
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
    ///     Gets the text for a given boundary
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

        for (var i = 0; i < LineEnds.Count; i++)
            if (LineEnds[i] >= index)
            {
                result.Index = LineStarts[i];
                result.Length = LineEnds[i] - LineStarts[i] + 1;
                break;
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

        var index = LineEnds[line];
        var bound = GetLineBoundary(index);
        return FullContent.Substring(bound.Index, bound.Length);
    }

    /// <summary>
    ///     Returns location (Line, Column) for given index in text
    /// </summary>
    /// <param name="index"> Position in text (line is one-indexed)</param>
    /// <returns> Location </returns>
    public Location GetLocation(int index)
    {
        for (var i = 1; i < LineEnds.Count; i++)
            if (LineEnds[i] >= index)
            {
                return new Location
                {
                    Column = index - LineStarts[i],
                    Line = i
                };
            }

        return new Location();
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
    /// <param name="scopes"> The scopes to check </param>
    /// <param name="boundary"> Boundary in the text </param>
    /// <returns> True if boundary is in a provided scope </returns>
    public bool ScopeMatch(IList<PatternScope> scopes, Boundary boundary)
    {
        if (scopes is null || !scopes.Any() || scopes.Contains(PatternScope.All))
        {
            return true;
        }

        if (scopes.Contains(PatternScope.All) || string.IsNullOrEmpty(prefix))
        {
            return true;
        }

        var isInComment = IsCommented(boundary.Index);

        return (!isInComment && scopes.Contains(PatternScope.Code)) ||
               (isInComment && scopes.Contains(PatternScope.Comment));
    }

    internal IEnumerable<(string, Boundary)> GetStringFromYmlPath(string Path)
    {
        lock (_yamlLock)
        {
            if (!_triedToConstructYmlDocument)
            {
                try
                {
                    _triedToConstructYmlDocument = true;
                    _ymlDocument = new YamlStream();
                    _ymlDocument.Load(new StringReader(FullContent));
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to parse {1} as a YML document: {0}", e.Message, _filePath);
                    _ymlDocument = null;
                }
            }
        }

        if (!(_ymlDocument?.Documents.Count > 0))
        {
            yield break;
        }

        var theDocuments = _ymlDocument.Documents.ToImmutableArray();
        foreach (var match in theDocuments.Select(document => document.RootNode.Query(Path)).SelectMany(matches => matches))
        {
            yield return (match.ToString(),
                new Boundary() { Index = match.Start.Index, Length = match.End.Index - match.Start.Index });
        }
    }
}