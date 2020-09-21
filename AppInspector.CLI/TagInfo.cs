// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DotLiquid;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Root parent for tag group preferences file and used in Writers\AnalyzeHtmlWriter.cs
    /// </summary>
    public class TagCategory
    {
        public enum tagInfoType { uniqueTags, allTags };

        [JsonProperty(PropertyName = "type")]
        public tagInfoType Type;

        [JsonProperty(PropertyName = "categoryName")]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "groups")]
        public List<TagGroup>? Groups { get; set; }

        public TagCategory()
        {
            Groups = new List<TagGroup>();
        }
    }

    /// <summary>
    /// Used to read customizable preference for Profile page e.g. rules\profile\profile.json
    /// </summary>
    public class TagGroup : Drop
    {
        [JsonProperty(PropertyName = "title")]
        public string? Title { get; set; }

        [JsonIgnore]
        public string? IconURL { get; set; }

        [JsonProperty(PropertyName = "dataRef")]
        public string? DataRef { get; set; }

        [JsonProperty(PropertyName = "patterns")]
        public List<TagSearchPattern>? Patterns { get; set; }

        public TagGroup()
        {
            Patterns = new List<TagSearchPattern>();
        }
    }

    public class TagSearchPattern : Drop
    {
        private string _searchPattern = "";
        private Regex? _expression;

        [JsonProperty(PropertyName = "searchPattern")]
        public string SearchPattern
        {
            get
            {
                return _searchPattern;
            }
            set
            {
                _searchPattern = value;
                _expression = null;
            }
        }

        public static bool ShouldSerializeExpression()
        {
            return false;
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

        [JsonProperty(PropertyName = "displayName")]
        public string? DisplayName { get; set; }

        [JsonProperty(PropertyName = "detectedIcon")]
        public string? DetectedIcon { get; set; } = "fas fa-cat";//default

        [JsonProperty(PropertyName = "notDetectedIcon")]
        public string? NotDetectedIcon { get; set; }

        [JsonProperty(PropertyName = "detected")]
        public bool Detected { get; set; }

        [JsonProperty(PropertyName = "details")]
        public string Details
        {
            get
            {
                string result = Detected ? "View" : "N/A";
                return result;
            }
        }

        [JsonProperty(PropertyName = "confidence")]
        public string Confidence { get; set; } = "Medium";
    }

    /// <summary>
    /// Primary use is development of lists of tags with specific group or pattern properties in reporting
    /// </summary>
    public class TagInfo : Drop
    {
        [JsonProperty(PropertyName = "tag")]
        public string? Tag { get; set; }

        [JsonProperty(PropertyName = "displayName")]
        public string? ShortTag { get; set; }

        [JsonIgnore]
        public string? StatusIcon { get; set; }

        private string _confidence = "Medium";

        [JsonProperty(PropertyName = "confidence")]
        public string Confidence
        {
            get => _confidence;
            set
            {
                RulesEngine.Confidence test;
                try
                {
                    if (Enum.TryParse(value, true, out test))
                    {
                        this._confidence = value;
                    }
                }
                catch (Exception)//control error description
                {
                    throw new Exception("Invalid argument value set attempt for Confidence");
                }
            }
        }

        [JsonProperty(PropertyName = "severity")]
        public string Severity { get; set; } = "Moderate";

        [JsonProperty(PropertyName = "detected")]
        public bool Detected { get; set; }
    }

    public class TagException
    {
        [JsonProperty(PropertyName = "tag")]
        public string? Tag { get; set; }
    }
}