// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
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
            RuleId = rule.Id;
            RuleName = rule.Name;
            RuleDescription = rule.Description;
            Tags = rule.Tags;
            Severity = rule.Severity;
        }

        [JsonConstructor]
        public MatchRecord(string ruleId, string ruleName)
        {
            RuleId = ruleId;
            RuleName = ruleName;
        }

        [JsonIgnore]
        [ExcludeFromCodeCoverage]
        public Rule? Rule { get; }

        /// <summary>
        /// Rule Id found in matching rule
        /// </summary>
        [JsonProperty(PropertyName ="ruleId")]
        [ExcludeFromCodeCoverage]
        public string RuleId { get; set; }

        /// <summary>
        /// Rule name found in matching rule
        /// </summary>
        [JsonProperty(PropertyName ="ruleName")]
        [ExcludeFromCodeCoverage]
        public string RuleName { get; set;  }

        /// <summary>
        /// Rule description found in matching rule
        /// </summary>
        [JsonProperty(PropertyName ="ruleDescription")]
        [ExcludeFromCodeCoverage]
        public string? RuleDescription { get; set; }

        /// <summary>
        /// Tags in matching rule
        /// </summary>
        [JsonProperty(PropertyName ="tags")]
        [ExcludeFromCodeCoverage]
        public string[]? Tags { get; set;  }

        /// <summary>
        /// Rule severity
        /// </summary>_rule
        [JsonProperty(PropertyName ="severity")]
        [ExcludeFromCodeCoverage]
        public Severity Severity { get; set;  }

        [JsonIgnore]
        [ExcludeFromCodeCoverage]
        public SearchPattern? MatchingPattern { get; set; }

        /// <summary>
        /// Matching pattern found in matching rule
        /// </summary>
        [JsonProperty(PropertyName ="pattern")]
        [ExcludeFromCodeCoverage]
        public string? Pattern => MatchingPattern?.Pattern;

        /// <summary>
        /// Pattern confidence in matching rule pattern
        /// </summary>
        [JsonProperty(PropertyName ="confidence")]
        [ExcludeFromCodeCoverage]
        public Confidence Confidence => MatchingPattern?.Confidence ?? Confidence.Unspecified;

        /// <summary>
        /// Pattern type of matching pattern
        /// </summary>
        [JsonProperty(PropertyName ="type")]
        [ExcludeFromCodeCoverage]
        public string? PatternType => MatchingPattern?.PatternType.ToString();

        [JsonIgnore]
        [ExcludeFromCodeCoverage]
        public TextContainer? FullTextContainer { get; set; }

        /// <summary>
        /// Internal to namespace only
        /// </summary>
        [JsonIgnore]
        [ExcludeFromCodeCoverage]
        public LanguageInfo LanguageInfo { get; set; } = new LanguageInfo();

        /// <summary>
        /// Friendly source type
        /// </summary>
        [JsonProperty(PropertyName ="language")]
        public string? Language => LanguageInfo?.Name;

        /// <summary>
        /// Filename of this match
        /// </summary>
        [JsonProperty(PropertyName ="fileName")]
        [ExcludeFromCodeCoverage]
        public string? FileName { get; set; }

        /// <summary>
        /// Matching text for this record
        /// </summary>
        [JsonProperty(PropertyName ="sample")]
        [ExcludeFromCodeCoverage]
        public string Sample { get; set; } = "";

        /// <summary>
        /// Matching surrounding context text for sample in this record
        /// </summary>
        [JsonProperty(PropertyName ="excerpt")]
        [ExcludeFromCodeCoverage]
        public string Excerpt { get; set; } = "";

        [JsonIgnore]
        [ExcludeFromCodeCoverage]
        public Boundary Boundary { get; set; } = new Boundary();

        /// <summary>
        /// Starting line location of the matching text
        /// </summary>
        [JsonProperty(PropertyName ="startLocationLine")]
        [ExcludeFromCodeCoverage]
        public int StartLocationLine { get; set; }

        /// <summary>
        /// Starting column location of the matching text
        /// </summary>
        [JsonProperty(PropertyName ="startLocationColumn")]
        [ExcludeFromCodeCoverage]
        public int StartLocationColumn { get; set; }

        /// <summary>
        /// Ending line location of the matching text
        /// </summary>
        [JsonProperty(PropertyName ="endLocationLine")]
        [ExcludeFromCodeCoverage]
        public int EndLocationLine { get; set; }

        /// <summary>
        /// Ending column of the matching text
        /// </summary>
        [JsonProperty(PropertyName ="endLocationColumn")]
        [ExcludeFromCodeCoverage]
        public int EndLocationColumn { get; set; }
    }
}