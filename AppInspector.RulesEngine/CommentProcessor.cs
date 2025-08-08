// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
/// Handles comment detection within text content
/// </summary>
internal class CommentProcessor
{
    private readonly string _content;
    private readonly string _prefix;
    private readonly string _suffix;
    private readonly string _inline;
    private readonly List<int> _lineStarts;
    private readonly List<int> _lineEnds;
    private readonly ConcurrentDictionary<int, bool> _commentedStates = new();

    public CommentProcessor(string content, string language, Languages languages, List<int> lineStarts, List<int> lineEnds)
    {
        _content = content;
        _lineStarts = lineStarts;
        _lineEnds = lineEnds;
        _prefix = languages.GetCommentPrefix(language);
        _suffix = languages.GetCommentSuffix(language);
        _inline = languages.GetCommentInline(language);
    }

    public bool HasCommentMarkers => !string.IsNullOrEmpty(_prefix);

    public bool IsCommented(int index)
    {
        if (!_commentedStates.ContainsKey(index))
        {
            PopulateCommentedState(index);
        }
        return _commentedStates[index];
    }

    private int GetPrefixLocation(int startOfLineIndex, int currentIndex, string prefix, bool multiline)
    {
        var prefixLoc = _content.LastIndexOf(prefix, currentIndex, System.StringComparison.Ordinal);
        if (prefixLoc != -1)
        {
            if (multiline)
            {
                return prefixLoc;
            }
            if (prefixLoc < startOfLineIndex)
            {
                return -1;
            }
            var numDoubleQuotes = _content[startOfLineIndex..prefixLoc].Count(x => x == '"');
            var numSingleQuotes = _content[startOfLineIndex..prefixLoc].Count(x => x == '\'');
            if (numDoubleQuotes % 2 == 1 || numSingleQuotes % 2 == 1)
            {
                if ((prefixLoc - 1) >= startOfLineIndex)
                {
                    return GetPrefixLocation(startOfLineIndex, prefixLoc - 1, prefix, multiline);
                }
                return -1;
            }
        }
        return prefixLoc;
    }

    private void PopulateCommentedStatesInternal(int index, string prefix, string suffix, bool multiline)
    {
        var startOfLine = GetLineBoundary(index);
        var prefixLoc = GetPrefixLocation(startOfLine.Index, index, prefix, multiline);

        if (prefixLoc != -1)
        {
            if (!_commentedStates.ContainsKey(prefixLoc))
            {
                var suffixLoc = _content.IndexOf(suffix, prefixLoc, System.StringComparison.Ordinal);
                if (suffixLoc == -1)
                {
                    suffixLoc = _content.Length - 1;
                }
                for (var i = prefixLoc; i <= suffixLoc; i++)
                {
                    _commentedStates[i] = true;
                }
            }
        }
    }

    public void PopulateCommentedState(int index)
    {
        var inIndex = index;
        if (index >= _content.Length)
        {
            index = _content.Length - 1;
        }
        if (index < 0)
        {
            index = 0;
        }

        if (!string.IsNullOrEmpty(_prefix) && !string.IsNullOrEmpty(_suffix))
        {
            PopulateCommentedStatesInternal(index, _prefix, _suffix, true);
        }
        if (!_commentedStates.ContainsKey(index) && !string.IsNullOrEmpty(_inline))
        {
            PopulateCommentedStatesInternal(index, _inline, "\n", false);
        }

        var i = index;
        while (!_commentedStates.ContainsKey(i) && i >= 0)
        {
            _commentedStates[i--] = false;
        }
        if (inIndex != index)
        {
            _commentedStates[inIndex] = _commentedStates[index];
        }
    }

    private Boundary GetLineBoundary(int index)
    {
        Boundary result = new();
        for (var i = 0; i < _lineEnds.Count; i++)
        {
            if (_lineEnds[i] >= index)
            {
                result.Index = _lineStarts[i];
                result.Length = _lineEnds[i] - _lineStarts[i] + 1;
                break;
            }
        }
        return result;
    }
}