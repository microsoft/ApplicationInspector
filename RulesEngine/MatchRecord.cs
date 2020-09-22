// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Represents augmented record of result issue from rules engine
    /// </summary>
    public class MatchRecord
    {
        /// <summary>
        /// Force required arg to avoid null rule
        /// </summary>
        /// <param name="rule"></param>
        public MatchRecord(Rule rule)
        {
            Rule = rule;
        }

        [JsonIgnore]
        public Rule Rule;

        /// <summary>
        /// Rule Id found in matching rule
        /// </summary>
        [JsonProperty(PropertyName = "ruleId")]
        public string RuleId => Rule.Id;

        /// <summary>
        /// Rule name found in matching rule
        /// </summary>
        [JsonProperty(PropertyName = "ruleName")]
        public string RuleName => Rule.Name;

        /// <summary>
        /// Rule description found in matching rule
        /// </summary>
        [JsonProperty(PropertyName = "ruleDescription")]
        public string? RuleDescription => Rule.Description;

        /// <summary>
        /// Tags in matching rule
        /// </summary>
        [JsonProperty(PropertyName = "tags")]
        public string[]? Tags => Rule.Tags;

        /// <summary>
        /// Rule severity
        /// </summary>_rule
        [JsonProperty(PropertyName = "severity")]
        public Severity Severity => Rule.Severity;

        [JsonIgnore]
        public SearchPattern? MatchingPattern { get; set; }

        /// <summary>
        /// Matching pattern found in matching rule
        /// </summary>
        [JsonProperty(PropertyName = "pattern")]
        public string? Pattern { get { return MatchingPattern?.Pattern; } }

        /// <summary>
        /// Pattern confidence in matching rule pattern
        /// </summary>
        [JsonProperty(PropertyName = "confidence")]
        public Confidence Confidence { get { return MatchingPattern?.Confidence ?? Confidence.Medium; } }

        /// <summary>
        /// Pattern type of matching pattern
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string? PatternType => MatchingPattern?.PatternType.ToString();

        /// <summary>
        /// Internal to namespace use only for capturing boundary excerpts and sample
        /// </summary>
        [JsonIgnore]
        public string? FullText { get; set; }

        /// <summary>
        /// Internal to namespace only 
        /// </summary>
        [JsonIgnore]
        public LanguageInfo LanguageInfo { get; set; } = new LanguageInfo();

        /// <summary>
        /// Friendly source type
        /// </summary>
        [JsonProperty(PropertyName = "language")]
        public string? Language => LanguageInfo?.Name;

        /// <summary>
        /// Filename of this match
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        public string? FileName { get; set; }

        /// <summary>
        /// Matching text for this record
        /// </summary>
        [JsonProperty(PropertyName = "sample")]
        public string Sample { get; set; } = "";

        /// <summary>
        /// Matching surrounding context text for sample in this record
        /// </summary>
        [JsonProperty(PropertyName = "excerpt")]
        public string Excerpt { get; set; } = "";

        [JsonIgnore]
        public Boundary Boundary { get; set; } = new Boundary();

        /// <summary>
        /// Starting line location of the matching text
        /// </summary>
        [JsonProperty(PropertyName = "startLocationLine")]
        public int StartLocationLine { get; set; }

        /// <summary>
        /// Starting column location of the matching text
        /// </summary>
        [JsonProperty(PropertyName = "startLocationColumn")]
        public int StartLocationColumn { get; set; }

        /// <summary>
        /// Ending line location of the matching text
        /// </summary>
        [JsonProperty(PropertyName = "endLocationLine")]
        public int EndLocationLine { get; set; }

        /// <summary>
        /// Ending column of the matching text
        /// </summary>
        [JsonProperty(PropertyName = "endLocationColumn")]
        public int EndLocationColumn { get; set; }
    }
}