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
using System.Xml.Linq;
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

    private bool _triedToConstructXDocument;
    private XDocument? _xDocument;
    private object _xdocumentLock = new();

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
                    // ele.ToString doesn't return the raw string from the json for booleans, it returns a capitalized False/True but JSON requires lower case false/true to parse
                    var eleString = ele.ValueKind is JsonValueKind.False ? "false" : ele.ValueKind is JsonValueKind.True ? "true" : ele.ToString();
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
    ///     If this file is XML, attempts to return the the string contents of the specified XPath applied to the file.
    ///     If the path does not exist, or the file is not XML returns empty enumeration.
    /// </summary>
    /// <param name="Path">XPath to query document with</param>
    /// <returns>Enumeration of string and Boundary tuples for the XPath matches. Boundary locations refer to the locations in the original document on disk.</returns>
    internal IEnumerable<(string, Boundary)> GetStringFromXPath(string Path, Dictionary<string, string> xpathNameSpaces)
    {
        // Lazy parse
        lock (_xdocumentLock)
        {
            if (!_triedToConstructXDocument)
            {
                try
                {
                    _triedToConstructXDocument = true;
                    _xDocument = XDocument.Parse(FullContent, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
                }
                catch (Exception e)
                {
                    _logger.LogError("Failed to parse {1} as a XML document: {0}", e.Message, _filePath);
                    _xDocument = null;
                }
            }
        }

        if (_xDocument is null)
        {
            yield break;
        }

        // Namespace manager
        var nt = new NameTable();
        var nsMgr = new XmlNamespaceManager(nt);
        
        // First, add namespaces from the document root element
        if (_xDocument.Root is not null)
        {
            foreach (var attr in _xDocument.Root.Attributes())
            {
                if (!attr.IsNamespaceDeclaration) continue;
                
                var prefix = attr.Name.LocalName;
                var namespaceUri = attr.Value;
                
                // Handle default namespace declaration (xmlns="...")
                if (attr.Name.Namespace == XNamespace.None && prefix == "xmlns")
                {
                    // This is a default namespace declaration
                    // Check if caller wants to map this to a specific prefix
                    var defaultMapping = xpathNameSpaces.FirstOrDefault(kvp => kvp.Value == namespaceUri);
                    if (defaultMapping.Key != null)
                    {
                        // Caller provided a mapping for this namespace URI, use their prefix
                        try { nsMgr.AddNamespace(defaultMapping.Key, namespaceUri); } catch { }
                    }
                    else
                    {
                        // Add as empty prefix for default namespace
                        try { nsMgr.AddNamespace(string.Empty, namespaceUri); } catch { }
                    }
                }
                else
                {
                    // Regular prefixed namespace declaration (xmlns:prefix="...")
                    // Only add if not already specified by caller
                    if (!xpathNameSpaces.ContainsKey(prefix))
                    {
                        try { nsMgr.AddNamespace(prefix, namespaceUri); } catch { }
                    }
                }
            }
        }
        
        // Then add caller supplied mappings (these can override document namespaces)
        foreach (var kvp in xpathNameSpaces)
        {
            try { nsMgr.AddNamespace(kvp.Key, kvp.Value); } catch { }
        }

        object? evalResult;
        try
        {
            evalResult = _xDocument.XPathEvaluate(Path, nsMgr);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("XPath evaluation failed for '{xpath}' on '{file}': {msg}", Path, _filePath, ex.Message);
            yield break;
        }

        IEnumerable<XObject> resultObjects = evalResult switch
        {
            IEnumerable<object?> seq => seq.OfType<XObject>(),
            XObject single => new[] { single },
            _ => Enumerable.Empty<XObject>()
        };

        // Track which element instances we've already mapped to avoid duplicate boundaries
        var usedElementPositions = new HashSet<int>();
        
        foreach (var obj in resultObjects)
        {
            string? value = obj switch
            {
                XElement e => e.Value,
                XAttribute a => a.Value,
                XText t => t.Value,
                _ => obj.ToString()
            };

            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            // Attempt line-info based boundary first
            if (obj is IXmlLineInfo li && li.HasLineInfo())
            {
                var line = li.LineNumber; // 1-based
                var col = li.LinePosition; // 1-based
                if (line < LineStarts.Count)
                {
                    var baseIdx = LineStarts[line] + (col - 1);
                    int mappedIndex = baseIdx;

                    if (obj is XAttribute attrObj)
                    {
                        // Attribute line position points to start of name. Find value within a local window.
                        var window = FullContent.AsSpan(baseIdx, Math.Min(400, FullContent.Length - baseIdx));
                        var possible = attrObj.Name.ToString() + "=";
                        var rel = window.IndexOf(possible, StringComparison.Ordinal);
                        if (rel >= 0)
                        {
                            var afterEq = baseIdx + rel + possible.Length;
                            if (afterEq < FullContent.Length)
                            {
                                var quote = FullContent[afterEq];
                                if (quote == '"' || quote == '\'')
                                {
                                    var valStart = afterEq + 1;
                                    if (valStart + value.Length <= FullContent.Length && FullContent.AsSpan(valStart, value.Length).SequenceEqual(value.AsSpan()))
                                    {
                                        mappedIndex = valStart;
                                        yield return (value, new Boundary { Index = mappedIndex, Length = value.Length });
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    else if (obj is XElement el && !el.HasElements)
                    {
                        // For a leaf element value, find the actual content between tags
                        // Use line info to get close to the right element, then find its specific content
                        var tagName = el.Name.LocalName;
                        
                        // Start searching from the line info position or a reasonable fallback
                        var searchStart = Math.Max(0, baseIdx - 100);
                        var searchEnd = Math.Min(FullContent.Length, baseIdx + 500);
                        
                        // Look for this specific element's opening tag
                        var currentPos = searchStart;
                        while (currentPos < searchEnd)
                        {
                            var tagPattern = "<" + tagName;
                            var tagIdx = FullContent.IndexOf(tagPattern, currentPos, StringComparison.Ordinal);
                            if (tagIdx < 0 || tagIdx >= searchEnd) break;
                            
                            // Find the end of the opening tag
                            var tagCloseIdx = FullContent.IndexOf('>', tagIdx);
                            if (tagCloseIdx >= 0 && tagCloseIdx + 1 < FullContent.Length)
                            {
                                var contentStart = tagCloseIdx + 1;
                                
                                // Skip any whitespace after the opening tag
                                while (contentStart < FullContent.Length && char.IsWhiteSpace(FullContent[contentStart]))
                                    contentStart++;
                                
                                // Check if the value is at this position
                                if (contentStart + value.Length <= FullContent.Length && 
                                    FullContent.AsSpan(contentStart, value.Length).SequenceEqual(value.AsSpan()))
                                {
                                    // Verify this is the right element by checking the closing tag follows
                                    var expectedCloseTag = "</" + tagName + ">";
                                    var afterContent = contentStart + value.Length;
                                    
                                    // Skip whitespace after content
                                    while (afterContent < FullContent.Length && char.IsWhiteSpace(FullContent[afterContent]))
                                        afterContent++;
                                    
                                    if (afterContent + expectedCloseTag.Length <= FullContent.Length &&
                                        FullContent.AsSpan(afterContent, expectedCloseTag.Length).SequenceEqual(expectedCloseTag.AsSpan()))
                                    {
                                        // Check if this position has been used already for duplicate element handling
                                        if (!usedElementPositions.Contains(contentStart))
                                        {
                                            usedElementPositions.Add(contentStart);
                                            mappedIndex = contentStart;
                                            yield return (value, new Boundary { Index = mappedIndex, Length = value.Length });
                                            goto nextObject; // Found this element, move to next
                                        }
                                        // If this position was already used, continue searching for the next instance
                                    }
                                }
                            }
                            
                            // Move past this tag to look for the next one
                            currentPos = tagIdx + tagPattern.Length;
                        }
                        
                        // Fallback: search forward a limited window for exact value
                        var fallbackWindow = Math.Min(800, FullContent.Length - baseIdx);
                        if (fallbackWindow > 0)
                        {
                            var span = FullContent.AsSpan(baseIdx, fallbackWindow);
                            var rel = span.IndexOf(value, StringComparison.Ordinal);
                            if (rel >= 0)
                            {
                                mappedIndex = baseIdx + rel;
                                yield return (value, new Boundary { Index = mappedIndex, Length = value.Length });
                                continue;
                            }
                        }
                    }
                }
            }

            // Fallback: context-aware global search
            if (obj is XAttribute attr)
            {
                // For attributes, do a more careful search for attr="value" pattern
                var attrName = attr.Name.LocalName;
                var attrPattern = attrName + "=\"" + value + "\"";
                var attrPatternSingle = attrName + "='" + value + "'";
                
                var idx1 = FullContent.IndexOf(attrPattern, StringComparison.Ordinal);
                var idx2 = FullContent.IndexOf(attrPatternSingle, StringComparison.Ordinal);
                
                int bestIdx = -1;
                if (idx1 >= 0 && idx2 >= 0)
                    bestIdx = Math.Min(idx1, idx2);
                else if (idx1 >= 0)
                    bestIdx = idx1;
                else if (idx2 >= 0)
                    bestIdx = idx2;
                
                if (bestIdx >= 0)
                {
                    var valueStart = bestIdx + attrName.Length + 2; // account for =" or ='
                    yield return (value, new Boundary { Index = valueStart, Length = value.Length });
                    continue;
                }
            }
            else if (obj is XElement elementObj)
            {
                // For elements, search for this specific element instance by looking at its position in the document
                var tagName = elementObj.Name.LocalName;
                
                // Find all instances of this element tag and try to match this specific one
                var allInstances = new List<int>();
                var searchPattern = "<" + tagName + ">";
                var pos = 0;
                
                while (pos < FullContent.Length)
                {
                    var idx = FullContent.IndexOf(searchPattern, pos, StringComparison.Ordinal);
                    if (idx < 0) break;
                    
                    var contentStart = idx + searchPattern.Length;
                    
                    // Skip whitespace
                    while (contentStart < FullContent.Length && char.IsWhiteSpace(FullContent[contentStart]))
                        contentStart++;
                    
                    // Check if the value matches at this position
                    if (contentStart + value.Length <= FullContent.Length &&
                        FullContent.AsSpan(contentStart, value.Length).SequenceEqual(value.AsSpan()))
                    {
                        // Verify the closing tag follows
                        var afterContent = contentStart + value.Length;
                        while (afterContent < FullContent.Length && char.IsWhiteSpace(FullContent[afterContent]))
                            afterContent++;
                        
                        var expectedCloseTag = "</" + tagName + ">";
                        if (afterContent + expectedCloseTag.Length <= FullContent.Length &&
                            FullContent.AsSpan(afterContent, expectedCloseTag.Length).SequenceEqual(expectedCloseTag.AsSpan()))
                        {
                            allInstances.Add(contentStart);
                        }
                    }
                    
                    pos = idx + 1;
                }
                
                // Try to find which instance this XElement corresponds to by using any available context
                // Use the first unused instance to ensure distinct boundaries
                if (allInstances.Count > 0)
                {
                    foreach (var instancePos in allInstances)
                    {
                        if (!usedElementPositions.Contains(instancePos))
                        {
                            usedElementPositions.Add(instancePos);
                            yield return (value, new Boundary { Index = instancePos, Length = value.Length });
                            goto nextObject;
                        }
                    }
                    
                    // If all instances are used, fall back to the first one
                    yield return (value, new Boundary { Index = allInstances[0], Length = value.Length });
                    continue;
                }
                
                // Fallback to the original pattern-based search
                var openPattern = ">" + value + "<";
                var wsPattern = ">" + value.Trim() + "<"; // Also try trimmed version
                
                var idx1 = FullContent.IndexOf(openPattern, StringComparison.Ordinal);
                var idx2 = FullContent.IndexOf(wsPattern, StringComparison.Ordinal);
                
                int bestIdx = -1;
                if (idx1 >= 0 && idx2 >= 0)
                    bestIdx = Math.Min(idx1, idx2);
                else if (idx1 >= 0)
                    bestIdx = idx1;
                else if (idx2 >= 0)
                    bestIdx = idx2;
                
                if (bestIdx >= 0)
                {
                    var valueStart = bestIdx + 1; // after the >
                    yield return (value, new Boundary { Index = valueStart, Length = value.Length });
                    continue;
                }
            }
            
            // Last resort: basic global search
            var globalIdx = FullContent.IndexOf(value, StringComparison.Ordinal);
            if (globalIdx >= 0)
            {
                yield return (value, new Boundary { Index = globalIdx, Length = value.Length });
            }
            
            nextObject:; // Label for goto statement
        }
    }

    /// <summary>
    /// Find the location of the provided prefix string if any in the text container between the provided start of line index and the current index
    /// </summary>
    /// <param name="startOfLineIndex">Minimum Character index in FullContent to locate Prefix</param>
    /// <param name="currentIndex">Maximal Chracter index in FullContent to locate Prefix</param>
    /// <param name="prefix">Prefix string to attempt to locate</param>
    /// <param name="multiline">If multi-line comments should be detected</param>
    /// <returns>The index of the specified prefix string in the FullContent if found, otherwise -1</returns>
    private int GetPrefixLocation(int startOfLineIndex, int currentIndex, string prefix, bool multiline)
    {
        // Find the first potential index of the prefix
        var prefixLoc = FullContent.LastIndexOf(prefix, currentIndex, StringComparison.Ordinal);
        if (prefixLoc != -1)
        {
            // TODO: Possibly support quoted multiline comment markers
            if (multiline)
            {
                return prefixLoc;
            }
            if (prefixLoc < startOfLineIndex)
            {
                return -1;
            }
            // Check how many quote marks occur on the line before the prefix location
            // TODO: This doesn't account for multi-line strings
            var numDoubleQuotes = FullContent[startOfLineIndex..prefixLoc].Count(x => x == '"');
            var numSingleQuotes = FullContent[startOfLineIndex..prefixLoc].Count(x => x == '\'');

            // If the number of quotes is odd, this is in a string, so not actually a comment prefix
            // It might be like var address = "http://contoso.com";
            if (numDoubleQuotes % 2 == 1 || numSingleQuotes % 2 == 1)
            {
                // The second argument is the maximal index to return since this calls LastIndexOf, subtract 1 to exclude this instance
                if ((prefixLoc -1) >= startOfLineIndex)
                {
                    return GetPrefixLocation(startOfLineIndex, prefixLoc - 1, prefix, multiline);
                }
                return -1;
            }
        }

        return prefixLoc;
    }

    /// <summary>
    ///     Populates the CommentedStates Dictionary based on the index and the provided comment prefix and suffix
    /// </summary>
    /// <param name="index">The character index in FullContent</param>
    /// <param name="prefix">The comment prefix</param>
    /// <param name="suffix">The comment suffix</param>
    private void PopulateCommentedStatesInternal(int index, string prefix, string suffix, bool multiline)
    {
        // Get the line boundary for the prefix location
        var startOfLine = GetLineBoundary(index);
        // Get the index of the prefix
        var prefixLoc = GetPrefixLocation(startOfLine.Index, index, prefix, multiline);

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
            PopulateCommentedStatesInternal(index, prefix, suffix, true);
        }

        // Populate true for indexes of the most immediately preceding instance of the single-line comment type if found
        if (!CommentedStates.ContainsKey(index) && !string.IsNullOrEmpty(inline))
        {
            PopulateCommentedStatesInternal(index, inline, "\n", false);
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
    ///     If the index is beyond the end of the file, clamps to the end
    /// </summary>
    /// <param name="index"> Position in text (line is one-indexed)</param>
    /// <returns> Location </returns>
    public Location GetLocation(int index)
    {
        for (var i = 1; i < LineEnds.Count; i++)
        {
            if (LineEnds[i] >= index)
            {
                return new Location
                {
                    Column = index - LineStarts[i],
                    Line = i
                };
            }
        }

        // If the index is beyond the end of the file, clamp to the end of the file
        if (index > LineEnds[^1])
        {
            return new Location()
            {
                Column = LineEnds[^1] - LineStarts[^1],
                Line = LineEnds.Count
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

        if (Languages.IsAlwaysCommented(Language))
        {
            return scopes.Contains(PatternScope.Comment);
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
            // TODO: Should update Boundary object to have longs intead of int rather than this casting
            yield return (match.ToString(),
                new Boundary() { Index = (int)match.Start.Index, Length = (int)match.End.Index - (int)match.Start.Index });
        }
    }
}