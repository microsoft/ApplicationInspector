// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Class to hold search pattern
    /// </summary>
    public class SearchPattern
    {
        private Dictionary<RegexOptions, Regex> _compiled = new();
        private string? _pattern;

        [JsonPropertyName("confidence")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Confidence Confidence { get; set; }

        [JsonPropertyName("modifiers")]
        public string[]? Modifiers { get; set; }

        [JsonPropertyName("pattern")]
        public string? Pattern
        {
            get
            {
                return _pattern;
            }
            set
            {
                _compiled.Clear();
                _pattern = value;
            }
        }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PatternType? PatternType { get; set; }

        [JsonPropertyName("scopes")]
        public PatternScope[]? Scopes { get; set; }
    }
}