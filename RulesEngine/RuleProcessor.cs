// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Heart of RulesEngine. Parses code applies rules
    /// </summary>
    public class RuleProcessor
    {
        private Confidence ConfidenceLevelFilter { get; set; }
        private readonly bool _stopAfterFirstMatch;
        private readonly bool _uniqueTagMatchesOnly;
        private readonly Logger? _logger;
        private readonly Analyzer analyzer;
        private readonly RuleSet _ruleset;
        private readonly ConcurrentDictionary<string, byte> _uniqueTagHashes;
        private readonly ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>> _rulesCache;

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
        public RuleProcessor(RuleSet rules, Confidence confidenceFilter, Logger? logger, bool uniqueMatches = false, bool stopAfterFirstMatch = false)
        {
            _ruleset = rules;
            EnableCache = true;
            _uniqueTagHashes = new ConcurrentDictionary<string,byte>();
            _rulesCache = new ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>>();
            _stopAfterFirstMatch = stopAfterFirstMatch;
            _uniqueTagMatchesOnly = uniqueMatches;
            _logger = logger;
            ConfidenceLevelFilter = confidenceFilter;
            SeverityLevel = Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice; //finds all; arg not currently supported

            analyzer = new Analyzer();
            analyzer.SetOperation(new WithinOperation(analyzer));
            analyzer.SetOperation(new OATRegexWithIndexOperation(analyzer)); //CHECK with OAT team as delegate doesn't appear to fire; ALT working fine in Analyze method anyway
        }

        /// <summary>
        /// Analyzes given line of code returning matching scan results 
        /// </summary>
        /// <param name="text">Source code</param>
        /// <param name="languages">List of languages</param>
        /// <returns>Array of matches</returns>
        public ScanResult[] Analyze(string text, LanguageInfo languageInfo)
        {
            // Get rules for the given content type
            IEnumerable<ConvertedOatRule> rules = GetRulesForSingleLanguage(languageInfo.Name).Where(x => !x.AppInspectorRule.Disabled && SeverityLevel.HasFlag(x.AppInspectorRule.Severity));
            List<ScanResult> resultsList = new List<ScanResult>();
            TextContainer textContainer = new TextContainer(text, languageInfo.Name);

            foreach (var ruleCapture in analyzer.GetCaptures(rules, textContainer))
            {
                // If we have within captures it means we had conditions, and we only want the conditioned captures
                var withinCaptures = ruleCapture.Captures.Where(x => x.Clause is WithinClause);
                if (withinCaptures.Any())
                {
                    foreach (var cap in withinCaptures)
                    {
                        ProcessBoundary(cap);
                    }
                }
                // Otherwise we can use all the captures
                else
                {
                    foreach (var cap in ruleCapture.Captures)
                    {
                        ProcessBoundary(cap);
                    }
                }

                void ProcessBoundary(ClauseCapture cap)
                {
                    List<ScanResult> newMatches = new List<ScanResult>();

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
                                    if (languageInfo.Type == LanguageInfo.LangFileType.Build && oatRule.AppInspectorRule.Tags.Any(v => !v.Contains("Metadata")))
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

                                    ScanResult newMatch = new ScanResult()
                                    {
                                        Boundary = boundary,
                                        StartLocation = textContainer.GetLocation(boundary.Index),
                                        EndLocation = textContainer.GetLocation(boundary.Index + boundary.Length),
                                        PatternMatch = oatRule.AppInspectorRule.Patterns[patternIndex],
                                        Confidence = oatRule.AppInspectorRule.Patterns[patternIndex].Confidence,
                                        Rule = oatRule.AppInspectorRule
                                    };

                                    bool addNewRecord = true;
                                    if (_uniqueTagMatchesOnly)
                                    {
                                        if (!RuleTagsAreUniqueOrAllowed(newMatch.Rule.Tags))
                                        {
                                            if (_stopAfterFirstMatch) 
                                            {
                                                addNewRecord = false; //we've seen already; don't improve the match
                                            }
                                            else if (newMatch.Confidence > Confidence.Low) //user prefers highest confidence match over first match
                                            {
                                                addNewRecord = BetterMatch(newMatches, newMatch) && BetterMatch(resultsList, newMatch);//check current rule matches and previous processed files
                                            }
                                        }
                                    }

                                    if (addNewRecord)
                                    {
                                        newMatches.Add(newMatch);
                                    }

                                    AddRuleTagHashes(oatRule.AppInspectorRule.Tags ?? new string[] { "" });
                                }
                            }
                        }
                    }

                    resultsList.AddRange(newMatches);
                }
            }

            if (resultsList.Any(x => x.Rule.Overrides!=null && x.Rule.Overrides.Length > 0 ))
            {
                // Deal with overrides
                List<ScanResult> removes = new List<ScanResult>();
                foreach (ScanResult m in resultsList)
                {
                    if (m.Rule.Overrides != null && m.Rule.Overrides.Length > 0)
                    {
                        foreach (string ovrd in m.Rule.Overrides)
                        {
                            // Find all overriden rules and mark them for removal from issues list
                            foreach (ScanResult om in resultsList.FindAll(x => x.Rule.Id == ovrd))
                            {
                                if (om.Boundary.Index >= m.Boundary.Index &&
                                    om.Boundary.Index <= m.Boundary.Index + m.Boundary.Length)
                                    removes.Add(om);
                            }
                        }
                    }
                }

                // Remove overriden rules
                resultsList.RemoveAll(x => removes.Contains(x));
            }

            return resultsList.ToArray();
        }

  
        #region Private Support Methods

        /// <summary>
        /// Identify if new scan result is a better match than previous i.e. has a higher confidence.  Assumes unique list of scanResults.
        /// </summary>
        /// <param name="scanResults"></param>
        /// <param name="compareResult"></param>
        /// <param name="removeOld"></param>
        /// <returns></returns>
        private bool BetterMatch(List<ScanResult> scanResults, ScanResult newScanResult, bool removeOld = true)
        {
            bool betterMatch = false;
            bool noMatch = true;

            //if list is empty the new match is the best match
            if (!scanResults.Any())
            {
                return true;
            }

            foreach (ScanResult scanResult in scanResults)
            {
                foreach (string scanResultTag in scanResult.Rule.Tags ?? new string[] {""})
                {
                    foreach (string newScanResultTag in newScanResult.Rule.Tags ?? new string[] {""})
                    {
                        if (scanResultTag == newScanResultTag)
                        {
                            noMatch = false;
                            if (newScanResult.Confidence > scanResult.Confidence)
                            {
                                if (removeOld)
                                {
                                    scanResults.Remove(scanResult);
                                }

                                betterMatch = true;
                                break;//as this method is used with uniquematche=true only one to worry about
                            }
                        }
                    }

                    if (betterMatch)
                    {
                        break;
                    }
                }

                if (betterMatch)
                {
                    break;
                }
            }

            return betterMatch || noMatch;
        }

        /// <summary>
        ///     Filters the rules for those matching the content type. Resolves all the overrides
        /// </summary>
        /// <param name="languages"> Languages to filter rules for </param>
        /// <returns> List of rules </returns>
        private IEnumerable<ConvertedOatRule> GetRulesForSingleLanguage(string language)
        {
            string langid = string.Empty;

            if (EnableCache)
            {
                // Make language id for cache purposes
                langid = string.Join(":", language);
                // Do we have the ruleset alrady in cache? If so return it
                if (_rulesCache.ContainsKey(langid))
                    return _rulesCache[langid];
            }

            IEnumerable<ConvertedOatRule> filteredRules = _ruleset.ByLanguage(language);

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
