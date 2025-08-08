// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using gfs.YamlDotNet.YamlPath;
using JsonCons.JsonPath;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YamlDotNet.RepresentationModel;
using Microsoft.ApplicationInspector.RulesEngine.Processors;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     Class to handle text as a searchable container
/// </summary>
public class TextContainer
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory? _loggerFactory; // store factory for processors

    private readonly string inline;
    private readonly string prefix;
    private readonly string suffix;

    private bool _triedToConstructJsonDocument;
    private JsonDocument? _jsonDocument;
    private object _jsonLock = new();

    private bool _triedToConstructYmlDocument;
    private YamlStream? _ymlDocument;
    private object _yamlLock = new();
    private readonly string? _filePath;

    private CommentProcessor? _commentProcessor;
    private XPathProcessor? _xPathProcessor; // new processor for XML/XPath

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
        _loggerFactory = loggerFactory; // save for processors
        _logger = loggerFactory?.CreateLogger<TextContainer>() ?? NullLogger<TextContainer>.Instance;
        Language = language;
        FullContent = content;
        LineEnds = new List<int> { 0 };
        LineStarts = new List<int> { 0, 0 };

        // Find line end in the text - handle both \r\n and \n line endings
        var pos = 0;
        while (pos < FullContent.Length)
        {
            var nextNewline = FullContent.IndexOf('\n', pos);
            if (nextNewline == -1) break;
            
            // For \r\n line endings, the line actually ends at the \r character
            var lineEndPos = (nextNewline > 0 && FullContent[nextNewline - 1] == '\r') 
                ? nextNewline - 1 
                : nextNewline;
            
            LineEnds.Add(lineEndPos);

            if (nextNewline + 1 < FullContent.Length)
            {
                LineStarts.Add(nextNewline + 1);
            }

            pos = nextNewline + 1;
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

    private void EnsureCommentProcessor()
    {
        _commentProcessor ??= new CommentProcessor(FullContent, Language, Languages, LineStarts, LineEnds);
    }

    private void EnsureXPathProcessor()
    {
        if (_xPathProcessor is null)
        {
            _xPathProcessor = new XPathProcessor(FullContent, _loggerFactory, _filePath, LineStarts);
        }
    }

    public bool IsCommented(int index)
    {
        EnsureCommentProcessor();
        return _commentProcessor!.IsCommented(index);
    }

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
        EnsureCommentProcessor();
        if (scopes.Contains(PatternScope.All) || !_commentProcessor!.HasCommentMarkers)
        {
            return true;
        }
        var isInComment = _commentProcessor.IsCommented(boundary.Index);
        return (!isInComment && scopes.Contains(PatternScope.Code)) || (isInComment && scopes.Contains(PatternScope.Comment));
    }

    /// <summary>
    ///     If this file is YAML, attempts to return the the string contents of the specified YamlPath applied to the file.
    ///     Method contains some heuristic behavior and may not cover all cases. 
    /// </summary>
    /// <param name="Path">YamlPath to query document with</param>
    /// <returns>Enumeration of string and Boundary tuples for the YamlPath matches. Boundary locations refer to the locations in the original document on disk.</returns>
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

    // Re-introduced helper methods required by other components (WithinOperation, RuleProcessor, Oat* operations)
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

    public Boundary GetLineBoundary(int index)
    {
        Boundary result = new();
        for (var i = 0; i < LineEnds.Count; i++)
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
        if (index > LineEnds[^1])
        {
            return new Location
            {
                Column = LineEnds[^1] - LineStarts[^1],
                Line = LineEnds.Count
            };
        }
        return new Location();
    }

    internal IEnumerable<(string, Boundary)> GetStringFromXPath(string path, Dictionary<string, string>? namespaces = null)
    {
        EnsureXPathProcessor();
        return _xPathProcessor!.GetMatches(path, namespaces);
    }
}