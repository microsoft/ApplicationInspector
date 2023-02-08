// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

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
}