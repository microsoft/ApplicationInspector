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
        public LanguageInfo Language { get; set; }

        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }
        [JsonProperty(PropertyName = "ruleId")]
        public string RuleId { get; set; }
        [JsonProperty(PropertyName = "ruleName")]
        public string RuleName { get; set; }
        [JsonProperty(PropertyName = "ruleDescription")]
        public string RuleDescription { get; set; }
        [JsonProperty(PropertyName = "pattern")]
        public string MatchingPattern { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string PatternType { get; set; }
        [JsonProperty(PropertyName = "confidence")]
        public string PatternConfidence { get; set; }
        [JsonProperty(PropertyName = "severity")]
        public string Severity { get; set; }
        [JsonProperty(PropertyName = "tags")]
        public string[] Tags { get; set; }
        [JsonProperty(PropertyName = "sourceLabel")]
        public string SourceLabel { get; set; }
        [JsonProperty(PropertyName = "sourceType")]
        public string SourceType { get; set; }
        [JsonProperty(PropertyName = "sample")]
        public string Sample { get; set; }
        [JsonProperty(PropertyName = "excerpt")]
        public string Excerpt { get; set; }
        [JsonProperty(PropertyName = "startLocationLine")]
        public int StartLocationLine { get; set; }
        [JsonProperty(PropertyName = "startLocationColumn")]
        public int StartLocationColumn { get; set; }
        [JsonProperty(PropertyName = "endLocationLine")]
        public int EndLocationLine { get; set; }
        [JsonProperty(PropertyName = "endLocationColumn")]
        public int EndLocationColumn { get; set; }
        [JsonProperty(PropertyName = "boundaryIndex")]
        public int BoundaryIndex { get; set; }
        [JsonProperty(PropertyName = "boundaryLength")]
        public int BoundaryLength { get; set; }


    }
}


