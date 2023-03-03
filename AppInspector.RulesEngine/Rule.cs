// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Class to hold the Rule
    /// </summary>
    public class Rule
    {
        private IList<Regex> _compiled = Array.Empty<Regex>();

        private IList<string>? _fileRegexes;
        private bool _updateCompiledFileRegex;

        /// <summary>
        ///     Name of the source where the rule definition came from.
        ///     Typically file, database or other storage.
        /// </summary>
        [JsonIgnore]
        public string? Source { get; set; }

        /// <summary>
        ///     Optional tag assigned to the rule during runtime
        /// </summary>
        [JsonIgnore]
        public string? RuntimeTag { get; set; }

        /// <summary>
        ///     Runtime flag to disable the rule
        /// </summary>
        [JsonIgnore]
        public bool Disabled { get; set; }

        /// <summary>
        /// Tags that are required to be present in the total result set - even from other rules matched against other files - for this match to be valid
        /// Does not work with `TagsOnly` option.
        /// </summary>
        [JsonPropertyName("depends_on_tags")] public IList<string>? DependsOnTags { get; set; }

        /// <summary>
        ///     Human readable name for the rule
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>
        ///     Id for the rule, by default IDs must be unique.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        /// <summary>
        ///     Human readable description for the rule
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; } = "";

        /// <summary>
        ///     Languages that the rule does not apply to
        /// </summary>
        [JsonPropertyName("does_not_apply_to")]
        public IList<string>? DoesNotApplyTo { get; set; }

        /// <summary>
        ///     Languages that the rule does apply to, if empty, applies to all languages not in <see cref="DoesNotApplyTo"/>
        /// </summary>
        [JsonPropertyName("applies_to")]
        public IList<string>? AppliesTo { get; set; }

        /// <summary>
        ///     Regular expressions for file names that the Rule applies to
        /// </summary>
        [JsonPropertyName("applies_to_file_regex")]
        public IList<string>? FileRegexes
        {
            get => _fileRegexes;
            set
            {
                _fileRegexes = value;
                _updateCompiledFileRegex = true;
            }
        }

        /// <summary>
        ///     Internal API to cache construction of <see cref="FileRegexes"/>
        /// </summary>
        [JsonIgnore]
        internal IEnumerable<Regex> CompiledFileRegexes
        {
            get
            {
                if (_updateCompiledFileRegex)
                {
                    _compiled = (IList<Regex>?)FileRegexes?.Select(x => new Regex(x, RegexOptions.Compiled)).ToList() ?? Array.Empty<Regex>();
                    _updateCompiledFileRegex = false;
                }

                return _compiled;
            }
        }

        /// <summary>
        ///     The Tags the rule provides
        /// </summary>
        [JsonPropertyName("tags")]
        public IList<string>? Tags { get; set; }

        /// <summary>
        ///     Severity for the rule
        /// </summary>
        [JsonPropertyName("severity")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Severity Severity { get; set; } = Severity.Moderate;

        /// <summary>
        ///     Other rules that this rule overrides
        /// </summary>
        [JsonPropertyName("overrides")]
        public IList<string>? Overrides { get; set; }

        /// <summary>
        ///     When any pattern matches, the rule applies
        /// </summary>
        [JsonPropertyName("patterns")]
        public SearchPattern[] Patterns { get; set; } = Array.Empty<SearchPattern>();

        /// <summary>
        ///     If any patterns match, and any conditions are set, all conditions must also match
        /// </summary>
        [JsonPropertyName("conditions")]
        public SearchCondition[]? Conditions { get; set; }

        /// <summary>
        ///     Optional list of self-test sample texts that the rule must match to be considered valid.
        /// </summary>
        [JsonPropertyName("must-match")]
        public IList<string>? MustMatch { get; set; }

        /// <summary>
        ///     Optional list of self-test sample texts that the rule must not match to be considered valid.
        /// </summary>
        [JsonPropertyName("must-not-match")]
        public IList<string>? MustNotMatch { get; set; }
    }
}