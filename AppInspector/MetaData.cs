// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Contains the analyze scanned meta data elements including rollup data for reporting purposes
    /// </summary>
    public class MetaData
    {
        [JsonIgnore]
        public Dictionary<string, HashSet<string>> KeyedPropertyLists { get; } //dynamic keyed list of properties with more than one value

        //simple properties 
        [JsonProperty(PropertyName = "applicationName")]
        public string ApplicationName { get; set; }
        [JsonProperty(PropertyName = "sourcePath")]
        public string SourcePath { get; set; }
        [JsonProperty(PropertyName = "sourceVersion")]
        public string SourceVersion { get; set; }
        [JsonProperty(PropertyName = "authors")]
        public string Authors { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        private readonly DateTime _lastUpdated = DateTime.MinValue;
        [JsonProperty(PropertyName = "lastUpdated")]
        public string LastUpdated { get; set; }
        [JsonProperty(PropertyName = "dateScanned")]
        public string DateScanned { get; set; }

        //stats
        [JsonProperty(PropertyName = "filesAnalyzed")]
        public int FilesAnalyzed { get; set; }
        [JsonProperty(PropertyName = "totalFiles")]
        public int TotalFiles { get; set; }
        [JsonProperty(PropertyName = "filesSkipped")]
        public int FilesSkipped { get; set; }
        [JsonProperty(PropertyName = "filesAffected")]
        public int FilesAffected { get; set; }
        //following "counter" methods can not use enumeration on matches list which are unique by default
        [JsonProperty(PropertyName = "totalMatchesCount")]
        public int TotalMatchesCount { get; set; }
        [JsonProperty(PropertyName = "uniqueMatchesCount")]
        public int UniqueMatchesCount => UniqueTags.Count;  //for liquid use

        //convenience getters for serialzation and easy reference of standard properties found in dynamic lists
        [JsonProperty(PropertyName = "packageTypes")]
        public HashSet<string> PackageTypes => KeyedPropertyLists["strGrpPackageTypes"];
        [JsonProperty(PropertyName = "appTypes")]
        public HashSet<string> AppTypes => KeyedPropertyLists["strGrpAppTypes"];
        [JsonIgnore]
        public HashSet<string> RulePaths { get => KeyedPropertyLists["strGrpRulePaths"]; set => KeyedPropertyLists["strGrpRulePaths"] = value; }
        [JsonIgnore]
        public HashSet<string> FileNames => KeyedPropertyLists["strGrpFileNames"];
        [JsonProperty(PropertyName = "uniqueTags")]
        public HashSet<string> UniqueTags { get => KeyedPropertyLists["strGrpUniqueTags"]; set => KeyedPropertyLists["strGrpUniqueTags"] = value; }
        [JsonProperty(PropertyName = "uniqueDependencies")]
        public HashSet<string> UniqueDependencies => KeyedPropertyLists["strGrpUniqueDependencies"];
        [JsonProperty(PropertyName = "outputs")]
        public HashSet<string> Outputs => KeyedPropertyLists["strGrpOutputs"];
        [JsonProperty(PropertyName = "targets")]
        public HashSet<string> Targets => KeyedPropertyLists["strGrpTargets"];
        [JsonProperty(PropertyName = "languages")]
        public Dictionary<string, int> Languages;
        [JsonProperty(PropertyName = "OSTargets")]
        public HashSet<string> OSTargets => KeyedPropertyLists["strGrpOSTargets"];
        [JsonProperty(PropertyName = "fileExtensions")]
        public HashSet<string> FileExtensions => KeyedPropertyLists["strGrpFileExtensions"];
        [JsonProperty(PropertyName = "cloudTargets")]
        public HashSet<string> CloudTargets => KeyedPropertyLists["strGrpCloudTargets"];
        [JsonProperty(PropertyName = "CPUTargets")]
        public HashSet<string> CPUTargets => KeyedPropertyLists["strGrpCPUTargets"];

        //other data types
        [JsonProperty(PropertyName = "tagCounters")]
        public List<MetricTagCounter> TagCounters { get; set; }
        [JsonProperty(PropertyName = "detailedMatchList")]
        public List<MatchRecord> MatchList { get; }//lighter formatted list structure more suited for json output to limit extraneous fieldo in Issues class


        public MetaData(string applicationName, string sourcePath)
        {
            ApplicationName = applicationName;

            SourcePath = sourcePath;

            //Initial value for ApplicationName may be replaced if rule pattern match found later
            MatchList = new List<MatchRecord>();

            //initialize standard set groups using dynamic lists variables that may have more than one value
            KeyedPropertyLists = new Dictionary<string, HashSet<string>>
            {
                ["strGrpPackageTypes"] = new HashSet<string>(),
                ["strGrpAppTypes"] = new HashSet<string>(),
                ["strGrpFileTypes"] = new HashSet<string>(),
                ["strGrpUniqueTags"] = new HashSet<string>(),
                ["strGrpOutputs"] = new HashSet<string>(),
                ["strGrpTargets"] = new HashSet<string>(),
                ["strGrpOSTargets"] = new HashSet<string>(),
                ["strGrpFileExtensions"] = new HashSet<string>(),
                ["strGrpFileNames"] = new HashSet<string>(),
                ["strGrpCPUTargets"] = new HashSet<string>(),
                ["strGrpCloudTargets"] = new HashSet<string>(),
                ["strGrpUniqueDependencies"] = new HashSet<string>()
            };

            Languages = new Dictionary<string, int>();
            TagCounters = new List<MetricTagCounter>();
        }


    }
}