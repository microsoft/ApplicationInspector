// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

[assembly: CLSCompliant(true)]

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Heart of RulesEngine. Parses code applies rules
    /// </summary>
    public class RuleProcessor
    {
        private Confidence ConfidenceLevelFilter { get; set; }
        private readonly bool _stopAfterFirstPatternMatch;
        private readonly bool _uniqueTagMatchesOnly;
        private readonly ConcurrentDictionary<string,byte> _uniqueTagHashes;
        private readonly Logger _logger;
        private RuleSet _ruleset;
        private Dictionary<string, IEnumerable<Rule>> _rulesCache;

        /// <summary>
        /// Support to exlude list of tags from unique restrictions
        /// </summary>
        public string[] UniqueTagExceptions { get; set; }

        /// <summary>
        /// Sets severity levels for analysis
        /// </summary>
        public Severity SeverityLevel { get; set; }

        /// <summary>
        /// Enables caching of rules queries if multiple reuses per instance
        /// </summary>
        public bool EnableCache { get; set; }

        /// <summary>
        /// Creates instance of RuleProcessor
        /// </summary>
        public RuleProcessor(RuleSet rules, Confidence confidence, bool uniqueTagMatches = false, bool stopAfterFirstPatternMatch = false, Logger logger = null)
        {
            _uniqueTagHashes = new ConcurrentDictionary<string,byte>();
            _stopAfterFirstPatternMatch = stopAfterFirstPatternMatch;
            _uniqueTagMatchesOnly = uniqueTagMatches;
            _logger = logger;

            EnableCache = false;
            ConfidenceLevelFilter = confidence;
            SeverityLevel = Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice; //find all; arg not currently supported

            if (rules == null)
            {
                throw new Exception("Null object.  No rules specified");
            }

            this.Rules = rules;
        }

        /// <summary>
        /// Analyzes given line of code
        /// </summary>
        /// <param name="text">Source code</param>
        /// <param name="languages">List of languages</param>
        /// <returns>Array of matches</returns>
        public ScanResult[] Analyze(string text, LanguageInfo languageInfo)
        {
            string[] languages = new string[] { languageInfo.Name };
            // Get rules for the given content type
            IEnumerable<Rule> rules = GetRulesForLanguages(languages);
            List<ScanResult> resultsList = new List<ScanResult>();
            TextContainer textContainer = new TextContainer(text, (languages.Length > 0) ? languages[0] : string.Empty, _stopAfterFirstPatternMatch);

            // Go through each rule
            foreach (Rule rule in rules)
            {
                if (_logger != null)
                {
                    _logger.Debug("Processing for rule: " + rule.Id);
                }

                // Skip rules that don't apply based on settings
                if (rule.Disabled || !SeverityLevel.HasFlag(rule.Severity))
                {
                    continue;
                }

                // Skip further processing of rule for efficiency if user requested first match only (speed/quality)
                if (_stopAfterFirstPatternMatch && _uniqueTagMatchesOnly && !UniqueTagsCheck(rule.Tags))
                {
                    continue;
                }

                List<ScanResult> matchList = new List<ScanResult>();

                // Go through each matching pattern of the rule
                foreach (SearchPattern pattern in rule.Patterns)
                {
                    //Skill patterns that don't apply based on settings
                    if (!ConfidenceLevelFilter.HasFlag(pattern.Confidence))
                    {
                        continue;
                    }

                    // Process all matches for the patttern (this may be only 1 is _stopAfterFirstMatch is set
                    foreach (Boundary match in textContainer.EnumerateMatchingBoundaries(pattern))
                    {
                        bool passedConditions = true;
                        foreach (SearchCondition condition in rule.Conditions)
                        {
                            bool res = textContainer.IsPatternMatch(condition.Pattern, match, condition);
                            if (res && condition.NegateFinding)
                            {
                                passedConditions = false;
                                break;
                            }
                            if (!res && condition.NegateFinding)
                            {
                                passedConditions = true;
                                break;
                            }
                            if (!res)
                            {
                                passedConditions = false;
                                break;
                            }
                        }

                        //restrict tags from build files to tags with "metadata" to avoid false feature positives that are not part of executable code
                        if (languageInfo.Type == LanguageInfo.LangFileType.Build && rule.Tags.Any(v => !v.Contains("Metadata")))
                        {
                            passedConditions = false;
                        }

                        if (passedConditions)
                        {
                            ScanResult newMatch = new ScanResult()
                            {
                                Boundary = match,
                                StartLocation = textContainer.GetLocation(match.Index),
                                EndLocation = textContainer.GetLocation(match.Index + match.Length),
                                PatternMatch = pattern,
                                Confidence = pattern.Confidence,
                                Rule = rule
                            };

                            if (_uniqueTagMatchesOnly)
                            {
                                if (!UniqueTagsCheck(newMatch.Rule.Tags)) //tag(s) previously seen
                                {
                                    if (_stopAfterFirstPatternMatch) //recheck stop at pattern level also within same rule
                                    {
                                        passedConditions = false; //user performance option i.e. only wants to identify if tag is detected nothing more
                                    }
                                    else if (newMatch.Confidence > Confidence.Low) //user prefers highest confidence match over first match
                                    {
                                        passedConditions = BestMatch(matchList, newMatch); //; check all patterns in current rule

                                        if (passedConditions)
                                        {
                                            passedConditions = BestMatch(resultsList, newMatch);//check all rules in permanent list
                                        }
                                    }
                                }
                            }

                            if (passedConditions)
                            {
                                matchList.Add(newMatch);
                            }

                            AddRuleTagHashes(rule.Tags);
                        }
                    }
                }

                resultsList.AddRange(matchList);
            }

            // Deal with overrides
            List<ScanResult> removes = new List<ScanResult>();
            foreach (ScanResult scanResult in resultsList)
            {
                if (scanResult.Rule.Overrides is string[] overrides)
                {
                    foreach (string @override in overrides)
                    {
                        // Find all overriden rules and mark them for removal from issues list
                        foreach (ScanResult overRideMatch in resultsList.FindAll(x => x.Rule.Id == @override))
                        {
                            if (overRideMatch.Boundary.Index >= scanResult.Boundary.Index &&
                                overRideMatch.Boundary.Index <= scanResult.Boundary.Index + scanResult.Boundary.Length)
                            {
                                removes.Add(overRideMatch);
                            }
                        }
                    }
                }
            }

            if (removes.Count > 0)
            {
                // Remove overriden rules
                resultsList.RemoveAll(x => removes.Contains(x));
            }

            return resultsList.ToArray();
        }

        /// <summary>
        /// Ruleset to be used for analysis
        /// </summary>
        public RuleSet Rules
        {
            get => _ruleset;
            set
            {
                _ruleset = value;
                _rulesCache = new Dictionary<string, IEnumerable<Rule>>();
            }
        }

        #region Private Methods

        private bool BestMatch(List<ScanResult> scanResults, ScanResult compareResult, bool removeOld = true)
        {
            bool betterMatch = false;

            //if list is empty the new match is the best match
            if (scanResults == null || scanResults.Count == 0)
            {
                return true;
            }

            foreach (ScanResult priorResult in scanResults)
            {
                foreach (string priorResultTag in priorResult.Rule.Tags)
                {
                    foreach (string newMatchTag in compareResult.Rule.Tags)
                    {
                        if (priorResultTag == newMatchTag)
                        {
                            if (compareResult.Confidence > priorResult.Confidence)
                            {
                                if (removeOld)
                                {
                                    scanResults.Remove(priorResult);
                                }

                                betterMatch = true;
                                break;
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

            return betterMatch;
        }

        /// <summary>
        /// Filters the rules for those matching the content type.
        /// Resolves all the overrides
        /// </summary>
        /// <param name="languages">Languages to filter rules for</param>
        /// <returns>List of rules</returns>
        private IEnumerable<Rule> GetRulesForLanguages(string[] languages)
        {
            string langid = string.Empty;

            if (EnableCache)
            {
                Array.Sort(languages);
                // Make language id for cache purposes
                langid = string.Join(":", languages);
                // Do we have the ruleset alrady in cache? If so return it
                if (_rulesCache.ContainsKey(langid))
                {
                    return _rulesCache[langid];
                }
            }

            IEnumerable<Rule> filteredRules = _ruleset.ByLanguages(languages);

            // Add the list to the cache so we save time on the next call
            if (EnableCache && filteredRules.Count() > 0)
            {
                _rulesCache.Add(langid, filteredRules);
            }

            return filteredRules;
        }

        /// <summary>
        /// Check if rule has at least one unique tag not seen before or if exception exists
        /// Assumes that _uniqueTagsOnly == true has been checked first for relevance
        /// </summary>
        /// <param name="ruleTags"></param>
        /// <returns></returns>
        private bool UniqueTagsCheck(string[] ruleTags)
        {
            bool approved = false;

            foreach (string tag in ruleTags)
            {
                if (_uniqueTagHashes.TryAdd(tag, 0))
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
            foreach (string t in ruleTags)
            {
                if (_uniqueTagHashes.TryAdd(t,0) && _logger != null)
                {
                    _logger.Debug(string.Format("Unique tag {0} added", t));
                }
            }
        }

        #endregion Private Methods
    }
}
