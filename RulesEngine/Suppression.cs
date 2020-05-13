// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Processor for rule suppressions
    /// </summary>
    public class Suppression
    {
        private const string KeywordPrefix = "RulesEngine:";
        private const string KeywordIgnore = "ignore";
        private const string KeywordAll = "all";
        private const string KeywordUntil = "until";
        private const string pattern = @"\s*" + KeywordPrefix + @"\s+" + KeywordIgnore + @"\s([a-zA-Z\d,:]+)(\s+" + KeywordUntil + @"\s\d{4}-\d{2}-\d{2}|)";

        private Regex reg = new Regex(pattern,RegexOptions.Compiled);
        private Regex dateReg = new Regex(@"(\d{4}-\d{2}-\d{2})",RegexOptions.Compiled);

        /// <summary>
        /// Creates new instance of Supressor
        /// </summary>
        /// <param name="text">Text to work with</param>
        public Suppression(string text)
        {
            if (text == null)
            {
#pragma warning disable IDE0016 // Use 'throw' expression - not supported in < C# 7
                throw new ArgumentNullException("text");
#pragma warning restore IDE0016 // Use 'throw' expression
            }
            _text = text;

            ParseLine();
        }

        /// <summary>
        /// Test if given rule Id is being suppressed
        /// </summary>
        /// <param name="issueId">Rule ID</param>
        /// <returns>True if rule is suppressed</returns>
        public SuppressedMatch GetSuppressedIssue(string issueId)
        {
            bool result = false;
            SuppressedMatch issue = _issues.FirstOrDefault(x => x.ID == issueId || x.ID == KeywordAll);
            if (issue != null)
            {
                result = true;
            }

            if (DateTime.Now < _expirationDate && result)
            {
                return issue;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Parse the line of code to find rule suppressors
        /// </summary>
        private void ParseLine()
        {
            // String.Contains is faster than RegEx. Quickly test if the further parsing is necessary or not
            if (!_text.Contains(KeywordPrefix))
            {
                return;
            }

            Match match = reg.Match(_text);

            if (match.Success)
            {
                _suppressStart = match.Index;
                _suppressLength = match.Length;

                string idString = match.Groups[1].Value.Trim();
                IssuesListIndex = match.Groups[1].Index;

                // Parse date
                if (match.Groups.Count > 2)
                {
                    string date = match.Groups[2].Value;
                    Match m = dateReg.Match(date);
                    if (m.Success)
                    {
                        _expirationDate = DateTime.ParseExact(m.Value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                // Parse Ids
                if (idString == KeywordAll)
                {
                    _issues.Add(new SuppressedMatch()
                    {
                        ID = KeywordAll,
                        Boundary = new Boundary()
                        {
                            Index = IssuesListIndex,
                            Length = KeywordAll.Length
                        }
                    });
                }
                else
                {
                    string[] ids = idString.Split(',');
                    int index = IssuesListIndex;
                    foreach (string id in ids)
                    {
                        _issues.Add(new SuppressedMatch()
                        {
                            ID = id,
                            Boundary = new Boundary()
                            {
                                Index = index,
                                Length = id.Length
                            }
                        });
                        index += id.Length + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Get issue IDs for the suppression
        /// </summary>
        /// <returns>List of issue IDs</returns>
        public virtual SuppressedMatch[] GetIssues()
        {
            return _issues.ToArray();
        }

        /// <summary>
        /// Validity of suppression expresion
        /// </summary>
        /// <returns>True if suppression is in effect</returns>
        public bool IsInEffect
        {
            get
            {
                bool doesItExists = (Index >= 0 && _issues.Count > 0);
                return (doesItExists && DateTime.Now < _expirationDate);
            }
        }

        /// <summary>
        /// Suppression expiration date
        /// </summary>
        public DateTime ExpirationDate => _expirationDate;

        /// <summary>
        /// Suppression expresion start index on the given line
        /// </summary>
        public int Index => _suppressStart;

        /// <summary>
        /// Suppression expression length
        /// </summary>
        public int Length => _suppressLength;

        /// <summary>
        /// Position of issues list
        /// </summary>
        public int IssuesListIndex { get; set; } = -1;

        private readonly List<SuppressedMatch> _issues = new List<SuppressedMatch>();
        private DateTime _expirationDate = DateTime.MaxValue;
        private readonly string _text = string.Empty;

        private int _suppressStart = -1;
        private int _suppressLength = -1;
    }
}