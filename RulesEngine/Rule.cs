// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Class to hold the Rule
    /// </summary>
    public class Rule
    {
        /// <summary>
        /// Name of the source where the rule definition came from.
        /// Typically file, database or other storage.
        /// </summary>
        [JsonIgnore]
        public string? Source { get; set; }

        /// <summary>
        /// Optional tag assigned to the rule during runtime
        /// </summary>
        [JsonIgnore]
        public string? RuntimeTag { get; set; }

        /// <summary>
        /// Runtime flag to disable the rule
        /// </summary>
        [JsonIgnore]
        public bool Disabled { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; } = "";

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = "";

        [JsonProperty(PropertyName = "description")]
        public string? Description { get; set; } = "";

        [JsonProperty(PropertyName = "applies_to")]
        public string[]? AppliesTo { get; set; }

            [JsonProperty(PropertyName = "tags")]
        public string[]? Tags { get; set; }

        [JsonProperty(PropertyName = "severity")]
        [JsonConverter(typeof(SeverityConverter))]
        public Severity Severity { get; set; } = Severity.Moderate;

        [JsonProperty(PropertyName = "overrides")]
        public string[]? Overrides { get; set; }

        [JsonProperty(PropertyName = "patterns")]
        public SearchPattern[] Patterns { get; set; } = new SearchPattern[] { };

        [JsonProperty(PropertyName = "conditions")]
        public SearchCondition[]? Conditions { get; set; }
    }
}