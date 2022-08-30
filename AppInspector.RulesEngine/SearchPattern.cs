// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     Class to hold search pattern
/// </summary>
public class SearchPattern
{
    [JsonProperty(PropertyName = "confidence")]
    [JsonConverter(typeof(StringEnumConverter))]
    public Confidence Confidence { get; set; }

    [JsonProperty(PropertyName = "modifiers")]
    public string[]? Modifiers { get; set; }

    [JsonProperty(PropertyName = "pattern")]
    public string? Pattern { get; set; }

    [JsonProperty(PropertyName = "type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public PatternType? PatternType { get; set; }

    [JsonProperty(PropertyName = "scopes")]
    public PatternScope[]? Scopes { get; set; }

    /// <summary>
    ///     If set, attempt to parse the file as XML  and if that is possible,
    ///     before running the pattern, select down to the XPath provided
    /// </summary>
    [JsonProperty(PropertyName = "xpaths")]
    public string[]? XPaths { get; set; }

    /// <summary>
    ///     If set, attempt to parse the file as JSON and if that is possible,
    ///     before running the pattern, select down to the JsonPath provided
    /// </summary>
    [JsonProperty(PropertyName = "jsonpaths")]
    public string[]? JsonPaths { get; set; }
}