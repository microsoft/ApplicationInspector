// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     Pattern Type for search pattern
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PatternType
{
    Regex,
    RegexWord,
    String,
    Substring
}