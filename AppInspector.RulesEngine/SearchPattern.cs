// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     Class to hold search pattern
/// </summary>
public class SearchPattern
{
    [JsonPropertyName("confidence")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Confidence Confidence { get; set; }

    [JsonPropertyName("modifiers")]
    public string[]? Modifiers { get; set; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PatternType? PatternType { get; set; }

    [JsonPropertyName("scopes")]
    public PatternScope[]? Scopes { get; set; }

    /// <summary>
    ///     If set, attempt to parse the file as XML  and if that is possible,
    ///     before running the pattern, select down to the XPath provided
    /// </summary>
    [JsonPropertyName("xpaths")]
    public string[]? XPaths { get; set; }

    /// <summary>
    ///     If set, attempt to parse the file as JSON and if that is possible,
    ///     before running the pattern, select down to the JsonPath provided
    /// </summary>
    [JsonPropertyName("jsonpaths")]
    public string[]? JsonPaths { get; set; }

    /// <summary>
    ///     If set, attempt to parse the file as YML and if that is possible,
    ///     before running the pattern, select down to the JsonPath provided
    /// </summary>
    [JsonPropertyName("ymlpaths")]
    public string[]? YamlPaths { get; set; }
}