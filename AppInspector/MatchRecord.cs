// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Represents augmented record of result issue from rules engine
    /// </summary>
    public class MatchRecord
    {
        /// <summary>
        /// Language details for this match
        /// </summary>
        public LanguageInfo Language { get; set; }

        /// <summary>
        /// Filename of this match
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// Rule Id found in matching rule
        /// </summary>
        [JsonProperty(PropertyName = "ruleId")]
        public string RuleId { get; set; }

        /// <summary>
        /// Rule name found in matching rule
        /// </summary>
        [JsonProperty(PropertyName = "ruleName")]
        public string RuleName { get; set; }

        /// <summary>
        /// Rule description found in matching rule
        /// </summary>
        [JsonProperty(PropertyName = "ruleDescription")]
        public string RuleDescription { get; set; }

        /// <summary>
        /// Matching pattern found in matching rule
        /// </summary>
        [JsonProperty(PropertyName = "pattern")]
        public string MatchingPattern { get; set; }

        /// <summary>
        /// Pattern type of matching pattern
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string PatternType { get; set; }

        /// <summary>
        /// Pattern confidence in matching rule pattern
        /// </summary>
        [JsonProperty(PropertyName = "confidence")]
        public string PatternConfidence { get; set; }

        /// <summary>
        /// Severity or importance value in matching rule
        /// </summary>
        [JsonProperty(PropertyName = "severity")]
        public string Severity { get; set; }

        /// <summary>
        /// Tags in matching rule
        /// </summary>
        [JsonProperty(PropertyName = "tags")]
        public string[] Tags { get; set; }

        /// <summary>
        /// Matching text for this record
        /// </summary>
        [JsonProperty(PropertyName = "sample")]
        public string Sample { get; set; }

        /// <summary>
        /// Matching surrounding context text for sample in this record
        /// </summary>
        [JsonProperty(PropertyName = "excerpt")]
        public string Excerpt { get; set; }

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