// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.RulesEngine;

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
    /// Tags that are required to be present from other rules - regardless of file - for this match to be valid
    /// </summary>
    [JsonPropertyName("depends_on_tags")] public IList<string>? DependsOnTags { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = "";

    [JsonPropertyName("id")] public string Id { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; } = "";

    [JsonPropertyName("does_not_apply_to")]
    public IList<string>? DoesNotApplyTo { get; set; }

    [JsonPropertyName("applies_to")]
    public IList<string>? AppliesTo { get; set; }

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

    [JsonIgnore]
    public IEnumerable<Regex> CompiledFileRegexes
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

    [JsonPropertyName("tags")] public IList<string>? Tags { get; set; }

    [JsonPropertyName("severity")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Severity Severity { get; set; } = Severity.Moderate;

    [JsonPropertyName("overrides")]
    public IList<string>? Overrides { get; set; }

    [JsonPropertyName("patterns")]
    public SearchPattern[] Patterns { get; set; } = Array.Empty<SearchPattern>();

    [JsonPropertyName("conditions")]
    public SearchCondition[]? Conditions { get; set; }

    [JsonPropertyName("must-match")]
    public IList<string>? MustMatch { get; set; }

    [JsonPropertyName("must-not-match")]
    public IList<string>? MustNotMatch { get; set; }
}