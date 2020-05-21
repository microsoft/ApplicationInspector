// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Contains the analyze scanned meta data elements including rollup data for reporting purposes
    /// </summary>
    public class MetaData
    {
        [JsonIgnore]
        public Dictionary<string, ConcurrentDictionary<string,byte>> KeyedPropertyLists { get; } //dynamic keyed list of properties with more than one value

        //simple properties
        /// <summary>
        /// Detected or derived project name
        /// </summary>
        [JsonProperty(PropertyName = "applicationName")]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Source path provided argument
        /// </summary>
        [JsonProperty(PropertyName = "sourcePath")]
        public string SourcePath { get; set; }

        /// <summary>
        /// Detected project source version
        /// </summary>
        [JsonProperty(PropertyName = "sourceVersion")]
        public string SourceVersion { get; set; }

        /// <summary>
        /// Detected source authors
        /// </summary>
        [JsonProperty(PropertyName = "authors")]
        public string Authors { get; set; }

        /// <summary>
        /// Detected source description
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        private readonly DateTime _lastUpdated = DateTime.MinValue;

        /// <summary>
        /// Last modified date for source code scanned
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdated")]
        public string LastUpdated { get; set; }

        /// <summary>
        /// Date of analyze scan
        /// </summary>
        [JsonProperty(PropertyName = "dateScanned")]
        public string DateScanned { get; set; }

        //stats

        /// <summary>
        /// Total number of files scanned successfully
        /// </summary>
        [JsonProperty(PropertyName = "filesAnalyzed")]
        public int FilesAnalyzed { get { return _FilesAnalyzed; } set { _FilesAnalyzed = value; } }

        private int _FilesAnalyzed;

        /// <summary>
        /// Total number of files in source path
        /// </summary>
        [JsonProperty(PropertyName = "totalFiles")]
        public int TotalFiles { get { return _TotalFiles; } set { _TotalFiles = value; } }

        private int _TotalFiles;

        /// <summary>
        /// Total number of skipped files based on supported formats
        /// </summary>
        [JsonProperty(PropertyName = "filesSkipped")]
        public int FilesSkipped { get { return _FilesSkipped; } set { _FilesSkipped = value; } }

        private int _FilesSkipped;

        /// <summary>
        /// Total files with at least one result
        /// </summary>
        [JsonProperty(PropertyName = "filesAffected")]
        public int FilesAffected { get { return _FilesAffected; } set { _FilesAffected = value; } }

        private int _FilesAffected;

        /// <summary>
        /// Total matches with supplied argument settings
        /// </summary>
        [JsonProperty(PropertyName = "totalMatchesCount")]
        public int TotalMatchesCount { get { return _TotalMatchesCount; } set { _TotalMatchesCount = value; } }

        private int _TotalMatchesCount;

        /// <summary>
        /// Total unique matches by tag
        /// </summary>
        [JsonProperty(PropertyName = "uniqueMatchesCount")]
        public int UniqueMatchesCount => UniqueTags.Count;  //for liquid use

        //convenience getters for serialzation and easy reference of standard properties found in dynamic lists

        /// <summary>
        /// List of detected package types 
        /// </summary>
        [JsonProperty(PropertyName = "packageTypes")]
        public ConcurrentDictionary<string,byte> PackageTypes => KeyedPropertyLists["strGrpPackageTypes"];

        /// <summary>
        /// List of detected application types
        /// </summary>
        [JsonProperty(PropertyName = "appTypes")]
        public ConcurrentDictionary<string,byte> AppTypes => KeyedPropertyLists["strGrpAppTypes"];

        [JsonIgnore]
        public ConcurrentDictionary<string,byte> RulePaths { get => KeyedPropertyLists["strGrpRulePaths"]; set => KeyedPropertyLists["strGrpRulePaths"] = value; }

        [JsonIgnore]
        public ConcurrentDictionary<string,byte> FileNames => KeyedPropertyLists["strGrpFileNames"];

        /// <summary>
        /// List of detected unique tags 
        /// </summary>
        [JsonProperty(PropertyName = "uniqueTags")]
        public ConcurrentDictionary<string,byte> UniqueTags { get => KeyedPropertyLists["strGrpUniqueTags"]; set => KeyedPropertyLists["strGrpUniqueTags"] = value; }

        /// <summary>
        /// List of detected unique code dependency includes
        /// </summary>
        [JsonProperty(PropertyName = "uniqueDependencies")]
        public ConcurrentDictionary<string,byte> UniqueDependencies => KeyedPropertyLists["strGrpUniqueDependencies"];

        /// <summary>
        /// List of detected output types
        /// </summary>
        [JsonProperty(PropertyName = "outputs")]
        public ConcurrentDictionary<string,byte> Outputs => KeyedPropertyLists["strGrpOutputs"];

        /// <summary>
        /// List of detected target types
        /// </summary>
        [JsonProperty(PropertyName = "targets")]
        public ConcurrentDictionary<string,byte> Targets => KeyedPropertyLists["strGrpTargets"];

        /// <summary>
        /// List of detected programming languages used
        /// </summary>
        [JsonProperty(PropertyName = "languages")]
        public ConcurrentDictionary<string, int> Languages;

        /// <summary>
        /// List of detected OS targets
        /// </summary>
        [JsonProperty(PropertyName = "OSTargets")]
        public ConcurrentDictionary<string,byte> OSTargets => KeyedPropertyLists["strGrpOSTargets"];

        /// <summary>
        /// LIst of detected file types (extension based)
        /// </summary>
        [JsonProperty(PropertyName = "fileExtensions")]
        public ConcurrentDictionary<string,byte> FileExtensions => KeyedPropertyLists["strGrpFileExtensions"];

        /// <summary>
        /// List of detected cloud host targets
        /// </summary>
        [JsonProperty(PropertyName = "cloudTargets")]
        public ConcurrentDictionary<string,byte> CloudTargets => KeyedPropertyLists["strGrpCloudTargets"];

        /// <summary>
        /// List of detected cpu targets
        /// </summary>
        [JsonProperty(PropertyName = "CPUTargets")]
        public ConcurrentDictionary<string,byte> CPUTargets => KeyedPropertyLists["strGrpCPUTargets"];

        //other data types

        /// <summary>
        /// List of detected tag counters i.e. metrics
        /// </summary>
        [JsonProperty(PropertyName = "tagCounters")]
        public List<MetricTagCounter> TagCounters { get; set; }

        /// <summary>
        /// List of detailed MatchRecords from scan
        /// </summary>
        [JsonProperty(PropertyName = "detailedMatchList")]
        public List<MatchRecord> Matches { get; }//lighter formatted list structure more suited for json output to limit extraneous fieldo in Issues class

        public MetaData(string applicationName, string sourcePath)
        {
            ApplicationName = applicationName;

            SourcePath = sourcePath;

            //Initial value for ApplicationName may be replaced if rule pattern match found later
            Matches = new List<MatchRecord>();

            //initialize standard set groups using dynamic lists variables that may have more than one value
            KeyedPropertyLists = new Dictionary<string, ConcurrentDictionary<string,byte>>
            {
                ["strGrpPackageTypes"] = new ConcurrentDictionary<string,byte>(),
                ["strGrpAppTypes"] = new ConcurrentDictionary<string,byte>(),
                ["strGrpFileTypes"] = new ConcurrentDictionary<string,byte>(),
                ["strGrpUniqueTags"] = new ConcurrentDictionary<string,byte>(),
                ["strGrpOutputs"] = new ConcurrentDictionary<string,byte>(),
                ["strGrpTargets"] = new ConcurrentDictionary<string,byte>(),
                ["strGrpOSTargets"] = new ConcurrentDictionary<string,byte>(),
                ["strGrpFileExtensions"] = new ConcurrentDictionary<string,byte>(),
                ["strGrpFileNames"] = new ConcurrentDictionary<string,byte>(),
                ["strGrpCPUTargets"] = new ConcurrentDictionary<string,byte>(),
                ["strGrpCloudTargets"] = new ConcurrentDictionary<string,byte>(),
                ["strGrpUniqueDependencies"] = new ConcurrentDictionary<string,byte>()
            };

            Languages = new ConcurrentDictionary<string, int>();
            TagCounters = new List<MetricTagCounter>();
        }

        internal void IncrementFilesAnalyzed(int amount = 1)
        {
            Interlocked.Add(ref _FilesAnalyzed, amount);
        }

        internal void IncrementTotalFiles(int amount = 1)
        {
            Interlocked.Add(ref _TotalFiles, amount);
        }

        internal void IncrementTotalMatchesCount(int amount = 1)
        {
            Interlocked.Add(ref _TotalMatchesCount, amount);
        }

        internal void IncrementFilesAffected(int amount = 1)
        {
            Interlocked.Add(ref _FilesAffected, amount);
        }

        internal void IncrementFilesSkipped(int amount = 1)
        {
            Interlocked.Add(ref _FilesSkipped, amount);
        }
    }
}