// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

        [JsonProperty(PropertyName ="confidence")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Confidence Confidence { get; set; }

        [JsonProperty(PropertyName ="modifiers")]
        public string[]? Modifiers { get; set; }

        [JsonProperty(PropertyName ="pattern")]
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

        [JsonProperty(PropertyName ="type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PatternType? PatternType { get; set; }

        [JsonProperty(PropertyName ="scopes")]
        public PatternScope[]? Scopes { get; set; }
    }
}