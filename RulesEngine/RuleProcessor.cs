// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CST.OAT;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Heart of RulesEngine. Parses code applies rules
    /// </summary>
    public class RuleProcessor
    {
        private readonly int MAX_TEXT_SAMPLE_LENGTH = 200;//char bytes

        private Confidence ConfidenceLevelFilter { get; set; }
        private readonly bool _stopAfterFirstMatch;
        private readonly bool _uniqueTagMatchesOnly;
        private readonly Logger? _logger;
        private readonly bool _treatEverythingAsCode;
        private readonly Analyzer analyzer;
        private readonly RuleSet _ruleset;
        private readonly ConcurrentDictionary<string, byte> _uniqueTagHashes;
        private readonly ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>> _rulesCache;
        private readonly ConcurrentQueue<MatchRecord> _runningResultsList;//maintain across files for bestmatch compare
        private readonly object _controllRunningListAdd;//safeguard shared list across threads
        /// <summary>
        /// Support to exlude list of tags from unique restrictions
        /// </summary>
        public string[]? UniqueTagExceptions { get; set; }

        /// <summary>
        /// Sets severity levels for analysis
        /// </summary>
        private Severity SeverityLevel { get; set; }

        /// <summary>
        /// Enables caching of rules queries if multiple reuses per instance
        /// </summary>
        private bool EnableCache { get; set; }

        /// <summary>
        /// Creates instance of RuleProcessor
        /// </summary>
        public RuleProcessor(RuleSet rules, Confidence confidenceFilter, Logger? logger, bool uniqueMatches = false, bool stopAfterFirstMatch = false, bool treatEverythingAsCode = false)
        {
            _ruleset = rules;
            EnableCache = true;
            _uniqueTagHashes = new ConcurrentDictionary<string,byte>();
            _rulesCache = new ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>>();
            _runningResultsList = new ConcurrentQueue<MatchRecord>();
            _controllRunningListAdd = new object();
            _stopAfterFirstMatch = stopAfterFirstMatch;
            _uniqueTagMatchesOnly = uniqueMatches;
            _logger = logger;
            _treatEverythingAsCode = treatEverythingAsCode;
            ConfidenceLevelFilter = confidenceFilter;
            SeverityLevel = Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice; //finds all; arg not currently supported

            analyzer = new Analyzer();
            analyzer.SetOperation(new WithinOperation(analyzer));
            analyzer.SetOperation(new OATRegexWithIndexOperation(analyzer)); //CHECK with OAT team as delegate doesn't appear to fire; ALT working fine in Analyze method anyway
        }

        public ConcurrentQueue<MatchRecord> AllResults => _runningResultsList;

        /// <summary>
        /// Analyzes given line of code returning matching scan results for the
        /// file passed in only; Use AllResults to get results across the entire set
        /// </summary>
        /// <param name="text">Source code</param>
        /// <param name="languages">List of languages</param>
        /// <returns>Array of matches</returns>
        public MatchRecord[] AnalyzeFile(string filePath, string text, LanguageInfo languageInfo)
        {
            // Get rules for the given content type
            var rules = GetRulesByRegex(languageInfo.Name).Where(x => !x.AppInspectorRule.Disabled && SeverityLevel.HasFlag(x.AppInspectorRule.Severity)).ToList();
            rules.AddRange(GetRulesByRegex(filePath).Where(x => !x.AppInspectorRule.Disabled && SeverityLevel.HasFlag(x.AppInspectorRule.Severity)));
            List<MatchRecord> resultsList = new List<MatchRecord>();//matches for this file only
            TextContainer textContainer = new TextContainer(text, languageInfo.Name);

            foreach (var ruleCapture in analyzer.GetCaptures(rules, textContainer))
            {

                foreach (var cap in ruleCapture.Captures)
                {
                    ProcessBoundary(cap);
                }

                void ProcessBoundary(ClauseCapture cap)
                {
                    List<MatchRecord> newMatches = new List<MatchRecord>();//matches for this rule clause only

                    if (cap is TypedClauseCapture<List<(int, Boundary)>> tcc)
                    {
                        if (ruleCapture.Rule is ConvertedOatRule oatRule)
                        {
                            if (tcc?.Result is List<(int, Boundary)> captureResults)
                            {
                                foreach (var match in captureResults)
                                {
                                    var patternIndex = match.Item1;
                                    var boundary = match.Item2;

                                    //restrict adds from build files to tags with "metadata" only to avoid false feature positives that are not part of executable code
                                    if (!_treatEverythingAsCode && languageInfo.Type == LanguageInfo.LangFileType.Build && oatRule.AppInspectorRule.Tags.Any(v => !v.Contains("Metadata")))
                                    {
                                        continue;
                                    }

                                    if (patternIndex < 0 || patternIndex > oatRule.AppInspectorRule.Patterns.Length)
                                    {
                                        _logger?.Error("Index out of range for patterns for rule: " + oatRule.AppInspectorRule.Name);
                                        continue;
                                    }

                                    if (!ConfidenceLevelFilter.HasFlag(oatRule.AppInspectorRule.Patterns[patternIndex].Confidence))
                                    {
                                        continue;
                                    }

                                    Location StartLocation = textContainer.GetLocation(boundary.Index);
                                    Location EndLocation = textContainer.GetLocation(boundary.Index + boundary.Length);
                                    MatchRecord newMatch = new MatchRecord(oatRule.AppInspectorRule)
                                    {
                                        FileName = filePath,
                                        FullText = textContainer.FullContent,
                                        LanguageInfo = languageInfo,
                                        Boundary = boundary,
                                        StartLocationLine = StartLocation.Line,
                                        EndLocationLine = EndLocation.Line != 0 ? EndLocation.Line : StartLocation.Line+1, //match is on last line
                                        MatchingPattern = oatRule.AppInspectorRule.Patterns[patternIndex],
                                        Excerpt = ExtractExcerpt(textContainer.FullContent, StartLocation.Line), 
                                        Sample = ExtractTextSample(textContainer.FullContent, boundary.Index, boundary.Length)
                                    };

                                    newMatches.Add(newMatch);

                                    if (oatRule.AppInspectorRule.Tags != null && oatRule.AppInspectorRule.Tags.Any())
                                    {
                                        AddRuleTagHashes(oatRule.AppInspectorRule.Tags);
                                    }
                                }
                            }
                        }
                    }

                    resultsList.AddRange(newMatches);
                }
            }

            if (_uniqueTagMatchesOnly)
            {
                var replacementList = new List<MatchRecord>();
                foreach (var entry in resultsList)
                {
                    if (!RuleTagsAreUniqueOrAllowed(entry.Rule.Tags))
                    {
                        if (!_stopAfterFirstMatch)
                        {
                            var replaceable = replacementList.Where(x => x.Tags.All(y => entry.Tags.Contains(y)));
                            if (replaceable.Any())
                            {
                                replaceable = replaceable.Where(x => x.Confidence < entry.Confidence).ToList();
                                if (replaceable.Any())
                                {
                                    replacementList.RemoveAll(x => replaceable.Contains(x));
                                    replacementList.Add(entry);
                                }
                            }
                            else
                            {
                                replacementList.Add(entry);
                            }
                        }
                    }
                    else
                    {
                        replacementList.Add(entry);
                    }
                }
                resultsList = replacementList;
            }
            List<MatchRecord> removes = new List<MatchRecord>();
            foreach (MatchRecord m in resultsList.Where(x => x.Rule.Overrides != null && x.Rule.Overrides.Length > 0))
            {
                if (m.Rule.Overrides != null && m.Rule.Overrides.Length > 0)
                {
                    foreach (string ovrd in m.Rule.Overrides)
                    {
                        // Find all overriden rules and mark them for removal from issues list
                        foreach (MatchRecord om in resultsList.FindAll(x => x.Rule.Id == ovrd))
                        {
                            if (om.Boundary?.Index >= m.Boundary?.Index &&
                                om.Boundary?.Index <= m.Boundary?.Index + m.Boundary?.Length)
                                removes.Add(om);
                        }
                    }
                }    
                // Remove overriden rules
            }
            resultsList.RemoveAll(x => removes.Contains(x));

            foreach (var entry in resultsList)
            {
                _runningResultsList.Enqueue(entry);
            }

            return resultsList.ToArray();
        }

  
        #region Private Support Methods

        /// <summary>
        ///     Filters the rules for those matching the content type. Resolves all the overrides
        /// </summary>
        /// <param name="languages"> Languages to filter rules for </param>
        /// <returns> List of rules </returns>
        private IEnumerable<ConvertedOatRule> GetRulesByRegex(string input)
        {
            string langid = string.Empty;

            if (EnableCache)
            {
                // Make language id for cache purposes
                langid = string.Join(":", input);
                // Do we have the ruleset alrady in cache? If so return it
                if (_rulesCache.ContainsKey(langid))
                    return _rulesCache[langid];
            }

            IEnumerable<ConvertedOatRule> filteredRules = _ruleset.ByRegex(input);

            // Add the list to the cache so we save time on the next call
            if (EnableCache && filteredRules.Any())
            {
                _rulesCache.TryAdd(langid, filteredRules);
            }

            return filteredRules;
        }

        /// <summary>
        /// Check if rule has at least one unique tag not seen before or if exception exists
        /// Assumes that _uniqueTagsOnly == true has been checked first for relevance
        /// </summary>
        /// <param name="ruleTags"></param>
        /// <returns></returns>
        private bool RuleTagsAreUniqueOrAllowed(string[]? ruleTags)
        {
            bool approved = false;
            if (ruleTags == null)
            {
                _logger?.Debug("Rule with no tags in RuleTagsAreUniqueOrAllowed method");
                throw new Exception("Rule with no tags in RuleTagsAreUniqueOrAllowed method");
            }

            foreach (string tag in ruleTags)
            {
                if (!_uniqueTagHashes.ContainsKey(tag))
                {
                    approved = true;
                    break;
                }
                else if (UniqueTagExceptions != null)
                {
                    foreach (string tagException in UniqueTagExceptions)
                    {
                        if (tag.Contains(tagException))
                        {
                            approved = true;
                            break;
                        }
                    }                   
                }
                
                if (_logger != null && !approved)
                {
                    _logger.Debug(string.Format("Duplicate tag {0} not approved for match", tag));
                }
            }

            return approved;
        }

        /// <summary>
        /// Simple wrapper but keeps calling code consistent
        /// Do not html code result which is accomplished later before out put to report
        /// </summary>
        private string ExtractTextSample(string fileText, int index, int length)
        {
            string result = "";
            try
            {
                //some js file results may be too long for practical display
                if (length > MAX_TEXT_SAMPLE_LENGTH)
                {
                    length = MAX_TEXT_SAMPLE_LENGTH;
                }

                result = fileText.Substring(index, length).Trim();
            }
            catch (Exception e)
            {
                _logger?.Error(e.Message + " in ExtractTextSample");
            }

            return result;
        }

        /// <summary>
        /// Located here to include during Match creation to avoid a call later or putting in constructor
        /// Needed in match ensuring value exists at time of report writing rather than expecting a callback
        /// from the template
        /// </summary>
        /// <returns></returns>
        private string ExtractExcerpt(string text, int startLineNumber, int length = 10)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            var lines = text.Split('\n');
            var distance = (int)((length - 1.0) / 2.0);

            // Sanity check
            if (startLineNumber < 0)
            {
                startLineNumber = 0;
            }

            if (startLineNumber >= lines.Length)
            {
                startLineNumber = lines.Length - 1;
            }

            var excerptStartLine = Math.Max(0, startLineNumber - distance);
            var excerptEndLine = Math.Min(lines.Length - 1, startLineNumber + distance);

            /* If the code snippet we're viewing is already indented 16 characters minimum,
             * we don't want to show all that extra white-space, so we'll find the smallest
             * number of spaces at the beginning of each line and use that.
             */

            var minSpaces = -1;
            for (var i = excerptStartLine; i <= excerptEndLine; i++)
            {
                var numPrefixSpaces = lines[i].TakeWhile(c => c == ' ').Count();
                minSpaces = (minSpaces == -1 || numPrefixSpaces < minSpaces) ? numPrefixSpaces : minSpaces;
            }

            var sb = new StringBuilder();
            // We want to go from (start - 5) to (start + 5) (off by one?)
            // LINE=10, len=5, we want 8..12, so N-(L-1)/2 to N+(L-1)/2
            // But cap those values at 0/end
            for (var i = excerptStartLine; i <= excerptEndLine; i++)
            {
                string line = lines[i].Substring(minSpaces).TrimEnd();
                sb.AppendLine(line);

            }

            return sb.ToString();
        }

        /// <summary>
        /// Supports unique tags option to not process if seen before
        /// No harm in being called again for same value; will not be added...
        /// </summary>
        /// <param name="ruleTags"></param>
        private void AddRuleTagHashes(string[] ruleTags)
        {
            foreach (string tag in ruleTags)
            {
                if (_uniqueTagHashes.TryAdd(tag,0) && _logger != null)
                {
                    _logger.Debug(string.Format("Unique tag {0} added", tag));
                }
            }
        }

        #endregion Private Methods
    }
}
