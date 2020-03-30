// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

[assembly: CLSCompliant(true)]
namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Heart of RulesEngine. Parses code applies rules
    /// </summary>
    public class RuleProcessor
    {
        private Confidence ConfidenceLevelFilter { get; set; }
        private bool _stopAfterFirstPatternMatch;
        private bool _uniqueTagMatchesOnly;
        private HashSet<string> _uniqueTagHashes;
        private Logger _logger;
        public string[] UniqueTagExceptions { get; set; }

        /// <summary>
        /// Creates instance of RuleProcessor
        /// </summary>
        public RuleProcessor(bool uniqueTagMatches = false, bool stopAfterFirstPatternMatch = false, Logger logger = null)
        {
            _rulesCache = new Dictionary<string, IEnumerable<Rule>>();
            EnableSuppressions = false;
            EnableCache = true;
            _stopAfterFirstPatternMatch = stopAfterFirstPatternMatch;

            _uniqueTagMatchesOnly = uniqueTagMatches;
            _uniqueTagHashes = new HashSet<string>();

            _logger = logger;

            SeverityLevel = Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice;
            ConfidenceLevelFilter = Confidence.High | Confidence.Medium | Confidence.Low;
        }

        /// <summary>
        /// Creates instance of RuleProcessor
        /// </summary>
        public RuleProcessor(RuleSet rules, Confidence confidence, bool uniqueTagMatches = false, bool stopAfterFirstPatternMatch = false, Logger logger = null) : this()
        {
            this.Rules = rules;
            EnableCache = false;
            EnableSuppressions = false;
            _stopAfterFirstPatternMatch = stopAfterFirstPatternMatch;

            _uniqueTagMatchesOnly = uniqueTagMatches;
            _uniqueTagHashes = new HashSet<string>();
            _logger = logger;

            ConfidenceLevelFilter = confidence;
        }

        #region Public Methods

        /// <summary>
        /// Applies given fix on the provided source code line
        /// </summary>
        /// <param name="text">Source code line</param>
        /// <param name="fixRecord">Fix record to be applied</param>
        /// <returns>Fixed source code line</returns>
        public static string Fix(string text, CodeFix fixRecord)
        {
            string result = string.Empty;

            if (fixRecord.FixType == FixType.RegexReplace)
            {
                //TODO: Better pattern search and modifiers
                Regex regex = new Regex(fixRecord.Pattern.Pattern);
                result = regex.Replace(text, fixRecord.Replacement);
            }

            return result;
        }


        /// <summary>
        /// Analyzes given line of code
        /// </summary>
        /// <param name="text">Source code</param>
        /// <param name="languages">List of languages</param>
        /// <returns>Array of matches</returns>
        public Issue[] Analyze(string text, LanguageInfo languageInfo)
        {
            string[] languages = new string[] { languageInfo.Name };
            // Get rules for the given content type
            IEnumerable<Rule> rules = GetRulesForLanguages(languages);
            List<Issue> resultsList = new List<Issue>();
            TextContainer textContainer = new TextContainer(text, (languages.Length > 0) ? languages[0] : string.Empty, _stopAfterFirstPatternMatch);

            // Go through each rule
            foreach (Rule rule in rules)
            {
                if (_logger != null)
                    _logger.Debug("Processing for rule: " + rule.Id);

                // Skip pattern matching this rule if uniquetag option and not in exceptions list
                bool multipleMatchesOk = !_uniqueTagMatchesOnly || UniqueTagsCheck(rule.Tags);
                if (!multipleMatchesOk)
                    continue;

                List<Issue> matchList = new List<Issue>();

                // Skip rules that don't apply based on settings
                if (rule.Disabled || !SeverityLevel.HasFlag(rule.Severity))
                    continue;

                // Go through each matching pattern of the rule
                foreach (SearchPattern pattern in rule.Patterns)
                {
                    //Skill patterns that don't apply based on settings
                    if (!ConfidenceLevelFilter.HasFlag(pattern.Confidence))
                        continue;

                    // Get all matches for the patttern
                    List<Boundary> matches = textContainer.MatchPattern(pattern);

                    if (matches.Count > 0)
                    {
                        foreach (Boundary match in matches)
                        {
                            bool passedConditions = true;
                            foreach (SearchCondition condition in rule.Conditions)
                            {
                                bool res = textContainer.MatchPattern(condition.Pattern, match, condition);
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

                            //do not accept features from build type files (only metadata) to avoid false positives that are not part of the executable program
                            if (languageInfo.Type == LanguageInfo.LangFileType.Build && rule.Tags.Any(v => !v.Contains("Metadata")))
                                passedConditions = false;

                            if (passedConditions)
                            {
                                Issue issue = new Issue()
                                {
                                    Boundary = match,
                                    StartLocation = textContainer.GetLocation(match.Index),
                                    EndLocation = textContainer.GetLocation(match.Index + match.Length),
                                    PatternMatch = pattern,
                                    Confidence = pattern.Confidence,
                                    Rule = rule
                                };

                                //check at pattern level to avoid adding duplicates 
                                if (_uniqueTagMatchesOnly && !UniqueTagsCheck(rule.Tags))
                                    break;

                                AddRuleTagHashes(rule.Tags);
                                matchList.Add(issue);
                            }
                        }
                    }

                }

                // We got matching rule and suppression are enabled,
                // let's see if we have a supression on the line
                if (EnableSuppressions && matchList.Count > 0)
                {
                    Suppression supp;
                    foreach (Issue result in matchList)
                    {
                        supp = new Suppression(textContainer.GetLineContent(result.StartLocation.Line));
                        // If rule is NOT being suppressed then report it
                        SuppressedIssue supissue = supp.GetSuppressedIssue(result.Rule.Id);
                        if (supissue == null)
                        {
                            resultsList.Add(result);
                        }
                        // Otherwise add the suppression info instead
                        else
                        {
                            Boundary bound = textContainer.GetLineBoundary(result.Boundary.Index);
                            bound.Index += supissue.Boundary.Index;
                            bound.Length = supissue.Boundary.Length;

                            //resultsList.Add();
                            Issue info = new Issue()
                            {
                                IsSuppressionInfo = true,
                                Boundary = bound,
                                StartLocation = textContainer.GetLocation(bound.Index),
                                EndLocation = textContainer.GetLocation(bound.Index + bound.Length),
                                Rule = result.Rule
                            };

                            // Add info only if it's not on the same location
                            if (resultsList.FirstOrDefault(x => x.Rule.Id == info.Rule.Id && x.Boundary.Index == info.Boundary.Index) == null)
                                resultsList.Add(info);
                            else if (_logger != null)
                                _logger.Debug("Not added due to proximity to another rule");

                        }
                    }

                }
                // Otherwise put matchlist to resultlist 
                else
                {
                    resultsList.AddRange(matchList);
                }
            }

            // Deal with overrides 
            List<Issue> removes = new List<Issue>();
            foreach (Issue m in resultsList)
            {
                if (m.Rule.Overrides != null && m.Rule.Overrides.Length > 0)
                {
                    foreach (string ovrd in m.Rule.Overrides)
                    {
                        // Find all overriden rules and mark them for removal from issues list   
                        foreach (Issue om in resultsList.FindAll(x => x.Rule.Id == ovrd))
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

            return resultsList.ToArray();
        }

        #endregion

        #region Private Methods      

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
                    return _rulesCache[langid];
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
        /// Check if rule tags have already been seen or if exception exists 
        /// Assumes that _uniqueTagsOnly == true has been checked first for relevance
        /// </summary>
        /// <param name="ruleTags"></param>
        /// <returns></returns>
        private bool UniqueTagsCheck(string[] ruleTags)
        {
            bool approved = true;

            foreach (string tag in ruleTags)
            {
                if (_uniqueTagHashes.Contains(tag))
                {
                    approved = false;
                    if (UniqueTagExceptions != null)
                    {
                        foreach (string tagException in UniqueTagExceptions)
                        {
                            approved = tag.Contains(tagException);
                            if (approved)
                                break;
                        }
                    }

                    break;
                }
                else if (_logger != null)
                    _logger.Debug(String.Format("Duplicate tag {0} not added", tag));
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
                _uniqueTagHashes.Add(t);

        }

        #endregion

        #region Properties

        /// <summary>
        /// Ruleset to be used for analysis
        /// </summary>
        public RuleSet Rules
        {
            get { return _ruleset; }
            set
            {
                _ruleset = value;
                _rulesCache = new Dictionary<string, IEnumerable<Rule>>();
            }
        }

        /// <summary>
        /// Sets severity levels for analysis
        /// </summary>
        public Severity SeverityLevel { get; set; }

        /// <summary>
        /// Enable suppresion syntax checking during analysis
        /// </summary>
        public bool EnableSuppressions { get; set; }

        /// <summary>
        /// Enables caching of rules queries.
        /// Increases performance and memory use!
        /// </summary>
        public bool EnableCache { get; set; }
        #endregion

        #region Fields 

        private RuleSet _ruleset;

        /// <summary>
        /// Cache for rules filtered by content type
        /// </summary>
        private Dictionary<string, IEnumerable<Rule>> _rulesCache;
        #endregion
    }
}
