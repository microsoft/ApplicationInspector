// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.RulesEngine;

public class SearchCondition
{
    [JsonPropertyName("negate_finding")]
    public bool NegateFinding { get; set; }

    [JsonPropertyName("pattern")]
    public SearchPattern? Pattern { get; set; }

    [JsonPropertyName("search_in")]
    public string? SearchIn { get; set; }

    /// <summary>
    ///     Restricts condition to specified languages. When empty, applies broadly (except DoesNotApplyTo).
    /// </summary>
    [JsonPropertyName("applies_to")]
    public IList<string>? AppliesTo { get; set; }

    /// <summary>
    ///     Prevents condition from being evaluated for specified languages.
    /// </summary>
    [JsonPropertyName("does_not_apply_to")]
    public IList<string>? DoesNotApplyTo { get; set; }

    /// <summary>
    ///     Optional name for this condition, for use in the rule's <c>expression</c>. Defaults to the
    ///     condition's clause index. May not contain spaces or parentheses, and must be unique within the rule.
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }
}