// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DotLiquid;
using Microsoft.ApplicationInspector.RulesEngine;

namespace Microsoft.ApplicationInspector.Commands;

/// <summary>
///     Root parent for tag group preferences file and used in Writers\AnalyzeHtmlWriter.cs
/// </summary>
public class TagCategory
{
    public enum tagInfoType
    {
        uniqueTags,
        allTags
    }

    [JsonPropertyName("type")]
    public tagInfoType Type;

    public TagCategory()
    {
        Groups = new List<TagGroup>();
    }

    [JsonPropertyName("categoryName")]
    public string? Name { get; set; }

    [JsonPropertyName("groups")]
    public List<TagGroup>? Groups { get; set; }
}

/// <summary>
///     Used to read customizable preference for Profile page e.g. rules\profile\profile.json
/// </summary>
public class TagGroup : Drop
{
    public TagGroup()
    {
        Patterns = new List<TagSearchPattern>();
    }

    [JsonPropertyName("title")] public string? Title { get; set; }

    [JsonIgnore] public string? IconURL { get; set; }

    [JsonPropertyName("dataRef")]
    public string? DataRef { get; set; }

    [JsonPropertyName("patterns")]
    public List<TagSearchPattern>? Patterns { get; set; }
}

public class TagSearchPattern : Drop
{
    private Regex? _expression;
    private string _searchPattern = "";

    [JsonPropertyName("searchPattern")]
    public string SearchPattern
    {
        get => _searchPattern;
        set
        {
            _searchPattern = value;
            _expression = null;
        }
    }

    public Regex Expression
    {
        get
        {
            if (_expression == null)
            {
                _expression = new Regex(SearchPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            return _expression;
        }
    }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("detectedIcon")]
    public string? DetectedIcon { get; set; } = "fas fa-cat"; //default

    [JsonPropertyName("notDetectedIcon")]
    public string? NotDetectedIcon { get; set; }

    [JsonPropertyName("detected")]
    public bool Detected { get; set; }

    [JsonPropertyName("details")]
    public string Details
    {
        get
        {
            var result = Detected ? "View" : "N/A";
            return result;
        }
    }

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = "Medium";

    public static bool ShouldSerializeExpression()
    {
        return false;
    }
}

/// <summary>
///     Primary use is development of lists of tags with specific group or pattern properties in reporting
/// </summary>
public class TagInfo : Drop
{
    private string _confidence = "Medium";

    [JsonPropertyName("tag")] public string? Tag { get; set; }

    [JsonPropertyName("displayName")]
    public string? ShortTag { get; set; }

    [JsonIgnore] public string? StatusIcon { get; set; }

    [JsonPropertyName("confidence")]
    public string Confidence
    {
        get => _confidence;
        set
        {
            if (Enum.TryParse(value, true, out Confidence test))
            {
                _confidence = value;
            }
        }
    }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "Moderate";

    [JsonPropertyName("detected")]
    public bool Detected { get; set; }
}

public class TagException
{
    [JsonPropertyName("tag")] public string? Tag { get; set; }
}