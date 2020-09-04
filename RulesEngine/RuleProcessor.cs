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
        private readonly bool _overRidesEnabled;
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
        public RuleProcessor(RuleSet rules, Confidence confidenceFilter, Logger? logger, bool uniqueMatches = false, bool stopAfterFirstMatch = false, bool overRidesAllowed=false)
        {
            if (rules == null)
            {
                throw new Exception("Null object.  No rules specified");
            }

            _ruleset = rules;
            EnableCache = true;
            _uniqueTagHashes = new ConcurrentDictionary<string,byte>();
            _rulesCache = new ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>>();
            _stopAfterFirstMatch = stopAfterFirstMatch;
            _uniqueTagMatchesOnly = uniqueMatches;
            _overRidesEnabled = overRidesAllowed;
            _logger = logger;
            ConfidenceLevelFilter = confidenceFilter;
            SeverityLevel = Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice; //finds all; arg not currently supported

            analyzer = new Analyzer();
            analyzer.SetOperation(new WithinOperation(analyzer));
            analyzer.SetOperation(new OATScopedRegexOperation(analyzer));
            //analyzer.SetOperation(new RegexWithIndexOperation(analyzer)); //CHECK with OAT team as delegate doesn't appear to fire
        }

        /// <summary>
        /// Analyzes given line of code returning matching scan results 
        /// </summary>
        /// <param name="text">Source code</param>
        /// <param name="languages">List of languages</param>
        /// <returns>Array of matches</returns>
        public ScanResult[] Analyze(string text, LanguageInfo languageInfo, int lineNumber = 0)
        {
            // Get rules for the given content type
            IEnumerable<ConvertedOatRule> rules = GetRulesForSingleLanguage(languageInfo.Name).Where(x => !x.AppInspectorRule.Disabled && SeverityLevel.HasFlag(x.AppInspectorRule.Severity));
            List<ScanResult> resultsList = new List<ScanResult>();
            TextContainer textContainer = new TextContainer(text, languageInfo.Name);

            IEnumerable<CST.OAT.Rule> ruleMatches = analyzer.Analyze(rules, textContainer);
            //bool found = ruleMatches.Any(x => x.Name.Equals("AI040300"));//debug test for missing expected tags

            foreach (var ruleCapture in analyzer.GetCaptures(rules, textContainer))
            {
                List<ScanResult> newMatches = new List<ScanResult>();
                // If we have within captures it means we had conditions, and we only want the conditioned captures
                var withinCaptures = ruleCapture.Captures.Where(x => x.Clause is WithinClause);
                if (withinCaptures.Any())
                {
                    foreach (var cap in withinCaptures)
                    {
                        ProcessBoundary(cap);
                    }
                    /* CHECK with OAT tool team on whether blocked out next lines should also be added to ensure we get both sets not one or the other.
                     * Note: "is not" not currently supported in 8.0 c# except in preview but can test of each excluding WithinClause
                    var withOutCaptures = ruleCapture.Captures.Where(x => x.Clause is not WithinClause);
                    foreach (var cap in withOutCaptures)
                    {
                        ProcessBoundary(cap);
                    }*/
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
                    if (cap is TypedClauseCapture<List<Boundary>> tcc)
                    {
                        if (ruleCapture.Rule is ConvertedOatRule orh)
                        {
                            foreach (var boundary in tcc.Result)
                            {
                                foreach (SearchPattern pattern in orh.AppInspectorRule.Patterns ?? new SearchPattern[] {}) 
                                {
                                    foreach (var clausePattern in tcc.Clause.Data)
                                    if (pattern.Pattern == clausePattern) //match OAT capture clause to AppInspector rule pattern for custom properties like Confidence
                                    { 
                                        //restrict tags build files to tags with "metadata" to avoid false feature positives that are not part of executable code
                                        if (languageInfo.Type == LanguageInfo.LangFileType.Build && orh.AppInspectorRule.Tags.Any(v => !v.Contains("Metadata")))
                                        {
                                            continue;
                                        }

                                        if (ConfidenceLevelFilter.HasFlag(pattern.Confidence))
                                        {
                                            ScanResult newMatch = new ScanResult()
                                            {
                                                Boundary = boundary,
                                                StartLocation = textContainer.GetLocation(boundary.Index),
                                                EndLocation = textContainer.GetLocation(boundary.Index + boundary.Length),
                                                PatternMatch = pattern,
                                                Confidence = pattern.Confidence,
                                                Rule = orh.AppInspectorRule
                                            };

                                            bool addNew = false;
                                            if (_uniqueTagMatchesOnly)
                                            {
                                                if (TagsAreUniqueOrAllowed(newMatch.Rule.Tags)) //tags not seen before or have exception like metrics
                                                {
                                                    addNew = true;
                                                }
                                                else if (!_stopAfterFirstMatch && BestMatch(newMatches, newMatch) && BestMatch(resultsList, newMatch))//best match option replacement found
                                                {
                                                    addNew = true;
                                                }
                                            }
                                            else
                                            {
                                                addNew = true;
                                            }

                                            if (addNew)
                                            {
                                                newMatches.Add(newMatch);
                                            }

                                            AddRuleTagHashes(orh.AppInspectorRule.Tags ?? new string[] {""});
                                        }
                                    }
                                    
                                }
                            }
                        }
                    }

                    resultsList.AddRange(newMatches);
                }
            }

            if (_overRidesEnabled)
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
        private bool BestMatch(List<ScanResult> scanResults, ScanResult newScanResult, bool removeOld = true)
        {
            bool betterMatch = false;
            bool noMatch = false;

            //if list is empty the new match is the best match
            if (scanResults == null || scanResults.Count == 0)
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
        private bool TagsAreUniqueOrAllowed(string[]? ruleTags)
        {
            bool approved = false;
            if (ruleTags == null)
            {
                if (_logger != null)
                {
                    _logger.Debug("Rule with no tags in TagsAreUniqueOrAllowed method");
                }
                throw new Exception("Missing rule tags found during processing");
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
