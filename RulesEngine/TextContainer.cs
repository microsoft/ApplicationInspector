// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInspector.RulesEngine
{
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
        /// <param name="lineNumber"> The line number to specify. Leave empty for full file as target. </param>
        public TextContainer(string content, string language)
        {
            Language = language;
            FullContent = content;
            LineEnds = new List<int>() { 0 };
            LineStarts = new List<int>() { 0, 0 };

            // Find line end in the text
            int pos = FullContent.IndexOf('\n');
            while (pos > -1)
            {
                LineEnds.Add(pos);

                if (pos > 0 && pos + 1 < FullContent.Length)
                {
                    LineStarts.Add(pos + 1);
                }
                pos = FullContent.IndexOf('\n', pos + 1);
            }

            if (LineEnds.Count < LineStarts.Count)
            {
                LineEnds.Add(FullContent.Length - 1);
            }

            prefix = RulesEngine.Language.GetCommentPrefix(Language);
            suffix = RulesEngine.Language.GetCommentSuffix(Language);
            inline = RulesEngine.Language.GetCommentInline(Language);
        }

        public string FullContent { get; }
        public string Language { get; }
        public List<int> LineEnds { get; }
        public List<int> LineStarts { get; }

        public ConcurrentDictionary<int,bool> CommentedStates { get; } = new();

        private void PopulateCommentedStatesInternal(int index, string prefix, string suffix)
        {
            var prefixLoc = FullContent.LastIndexOf(prefix, index);
            if (prefixLoc != -1)
            {
                if (!CommentedStates.ContainsKey(prefixLoc))
                {
                    var suffixLoc = FullContent.IndexOf(suffix, prefixLoc);
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
            if (!string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(suffix))
            {
                PopulateCommentedStatesInternal(index, prefix, suffix);
            }
            if (!CommentedStates.ContainsKey(index) && !string.IsNullOrEmpty(inline))
            {
                PopulateCommentedStatesInternal(index, inline, "\n");
            }
            if (!CommentedStates.ContainsKey(index))
            {
                CommentedStates[index] = false;
            }
            if (inIndex != index)
            {
                CommentedStates[inIndex] = CommentedStates[index];
            }
        }

        public string GetBoundaryText(Boundary capture)
        {
            if (capture is null)
            {
                return string.Empty;
            }
            return FullContent[(Math.Min(FullContent.Length, capture.Index))..(Math.Min(FullContent.Length, capture.Index + capture.Length))];
        }

        /// <summary>
        ///     Returns boundary for a given index in text
        /// </summary>
        /// <param name="index"> Position in text </param>
        /// <returns> Boundary </returns>
        public Boundary GetLineBoundary(int index)
        {
            Boundary result = new Boundary();

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
        /// <param name="line"> Line number </param>
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
        /// <param name="index"> Position in text </param>
        /// <returns> Location </returns>
        public Location GetLocation(int index)
        {
            Location result = new Location();

            if (index == 0)
            {
                result.Line = 1;
                result.Column = 1;
            }
            else
            {
                for (int i = 0; i < LineEnds.Count; i++)
                {
                    if (LineEnds[i] >= index)
                    {
                        result.Line = i;
                        result.Column = index - LineStarts[i];
                        break;
                    }
                }
            }
            return result;
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
        /// <param name="pattern"> Pattern with scope </param>
        /// <param name="boundary"> Boundary in a text </param>
        /// <param name="text"> Text </param>
        /// <returns> True if boundary is matching the pattern scope </returns>
        public bool ScopeMatch(IEnumerable<PatternScope> patterns, Boundary boundary)
        {
            if (patterns is null)
            {
                return true;
            }
            if (patterns.Contains(PatternScope.All) || string.IsNullOrEmpty(prefix))
                return true;
            bool isInComment = IsCommented(boundary.Index);

            return (!isInComment && patterns.Contains(PatternScope.Code)) || (isInComment && patterns.Contains(PatternScope.Comment));
        }

        private string inline;
        private string prefix;
        private string suffix;
    }
}