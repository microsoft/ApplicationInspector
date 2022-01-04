// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
        public string[]? AppliesTo
        {
            get
            {
                return _appliesTo;
            }
            set
            {
                _updateCompiled = true;
                _appliesTo = value;
            }
        }

        private string[]? _appliesTo;

        [JsonProperty(PropertyName = "applies_to_file_regex")]
        public string[]? FileRegexes
        {
            get => _fileRegexes;
            set
            {
                _fileRegexes = value;
                _updateCompiledFileRegex = true;
            }
        }

        private string[]? _fileRegexes;

        [JsonIgnore]
        public IEnumerable<Regex> CompiledFileRegexes
        {
            get
            {
                if (_updateCompiledFileRegex)
                {
                    _compiled = FileRegexes?.Select(x => new Regex(x, RegexOptions.Compiled)) ?? Array.Empty<Regex>();
                    _updateCompiled = false;
                }
                return _compiled;
            }
        }

        private IEnumerable<Regex> _compiled = Array.Empty<Regex>();
        private bool _updateCompiled = false;
        private bool _updateCompiledFileRegex = false;

        [JsonProperty(PropertyName = "tags")]
        public string[]? Tags { get; set; }

        [JsonProperty(PropertyName = "severity")]
        public Severity Severity { get; set; } = Severity.Moderate;

        [JsonProperty(PropertyName = "overrides")]
        public string[]? Overrides { get; set; }

        [JsonProperty(PropertyName = "patterns")]
        public SearchPattern[] Patterns { get; set; } = Array.Empty<SearchPattern>();

        [JsonProperty(PropertyName = "conditions")]
        public SearchCondition[]? Conditions { get; set; }
    }
}