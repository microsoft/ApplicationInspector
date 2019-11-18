// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using DotLiquid;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.AppInspector.Writers
{
    /// <summary>
    /// Root parent for tag group preferences file
    /// </summary>
    [Serializable]
    public class TagCategory
    {
        public enum tagInfoType { uniqueTags, allTags };
        [JsonProperty(PropertyName = "type")]
        public tagInfoType Type;
        [JsonProperty(PropertyName = "categoryName")]
        public String Name { get; set; }
        [JsonProperty(PropertyName = "groups")]
        public List<TagGroup> Groups { get; set; }

        public TagCategory()
        {
            Groups = new List<TagGroup>();
        }
    }

    /// <summary>
    /// Used to read customizable preference for Profile page e.g. rules\profile\profile.json
    /// </summary>
    [Serializable]
    public class TagGroup : Drop
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }
        [JsonIgnore]
        public string IconURL { get; set; }
        [JsonProperty(PropertyName = "dataRef")]
        public string DataRef { get; set; }
        [JsonProperty(PropertyName = "patterns")]
        public List<TagSearchPattern> Patterns { get; set; }

        public TagGroup()
        {
            Patterns = new List<TagSearchPattern>();
        }
    }


    [Serializable]
    public class TagSearchPattern : Drop
    {
        [JsonProperty(PropertyName = "searchPattern")]
        public string SearchPattern { get; set; }
        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; }
        [JsonProperty(PropertyName = "detectedIcon")]
        public string DetectedIcon { get; set; }
        [JsonProperty(PropertyName = "notDetectedIcon")]
        public string NotDetectedIcon { get; set; }
        [JsonProperty(PropertyName = "detected")]
        public bool Detected { get; set; }
        [JsonProperty(PropertyName = "details")]
        public string Details { get 
            {
                string result = Detected == true ? "View" : "N/A";
                return result;
            }
        }
        [JsonProperty(PropertyName = "confidence")]
        public string Confidence { get; set; }

        public TagSearchPattern()
        {
            DetectedIcon = "fas fa-cat";//default
        }
    }

    /// <summary>
    /// Primary use is development of lists of tags with specific group or pattern properties in reporting
    /// </summary>
    public class TagInfo : Drop
    {
        public string Tag { get; set; }
        public string ShortTag { get; set; }
        [JsonIgnore]
        public string StatusIcon { get; set; }
        private string _confidence;
        public string Confidence
        {
            get { return _confidence; }
            set
            {
                RulesEngine.Confidence test;
                try
                {
                    if (Enum.TryParse(value, true, out test))
                        this._confidence = value;
                }
                catch (Exception)//control error description
                {
                    throw new Exception("Invalid argument value set attempt for Confidence");
                }

            }
        }
        public string Severity { get; set; }
        public bool Detected { get; set; }
       
    }

    public class TagCounterUI : Drop
    {
        [JsonProperty(PropertyName = "tag")]
        public string Tag { get; set; }
        [JsonProperty(PropertyName = "displayName")]
        public string ShortTag { get; set; }
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }
        [JsonProperty(PropertyName = "includeAsMatch")]
        public bool IncludeAsMatch { get; set; }
    }

    public class TagCounter 
    {
        [JsonProperty(PropertyName = "tag")]
        public string Tag { get; set; }
        [JsonProperty(PropertyName = "displayName")]
        public string ShortTag { get; set; }
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }
        [JsonProperty(PropertyName = "includeAsMatch")]
        public bool IncludeAsMatch { get; set; }
    }


    [Serializable]
    public class TagException
    {
        [JsonProperty(PropertyName = "tag")]
        public string Tag { get; set; }       
    }

}


