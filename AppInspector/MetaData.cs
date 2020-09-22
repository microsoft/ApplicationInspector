// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Contains the analyze scanned meta data elements including rollup data for reporting purposes
    /// </summary>
    public class MetaData
    {
        //simple properties
        /// <summary>
        /// Detected or derived project name
        /// </summary>
        [JsonProperty(PropertyName = "applicationName")]
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Source path provided argument
        /// </summary>
        [JsonProperty(PropertyName = "sourcePath")]
        public string? SourcePath { get; set; }

        /// <summary>
        /// Detected project source version
        /// </summary>
        [JsonProperty(PropertyName = "sourceVersion")]
        public string? SourceVersion { get; set; }

        /// <summary>
        /// Detected source authors
        /// </summary>
        [JsonProperty(PropertyName = "authors")]
        public string? Authors { get; set; }

        /// <summary>
        /// Detected source description
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string? Description { get; set; }

        private readonly DateTime _lastUpdated = DateTime.MinValue;

        /// <summary>
        /// Last modified date for source code scanned
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdated")]
        public string? LastUpdated { get; set; }

        /// <summary>
        /// Date of analyze scan
        /// </summary>
        [JsonProperty(PropertyName = "dateScanned")]
        public string? DateScanned { get; set; }

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
        public int UniqueMatchesCount { 
            get 
            {
                if (UniqueTags != null)
                    return UniqueTags.Count;
                else 
                    return 0; 
            } 
        }

        /// <summary>
        /// List of detected package types 
        /// </summary>
        [JsonProperty(PropertyName = "packageTypes")]
        public List<string>? PackageTypes { get; set; } = new List<string>();

        /// <summary>
        /// List of detected application types
        /// </summary>
        [JsonProperty(PropertyName = "appTypes")]
        public List<string>? AppTypes { get; set; } = new List<string>();

        /// <summary>
        /// List of detected unique tags 
        /// </summary>
        [JsonProperty(PropertyName = "uniqueTags")]
        public List<string>? UniqueTags { get; set; } = new List<string>();


        /// <summary>
        /// List of detected unique code dependency includes
        /// </summary>
        [JsonProperty(PropertyName = "uniqueDependencies")]
        public List<string>? UniqueDependencies { get; set; } = new List<string>();

        /// <summary>
        /// List of detected output types
        /// </summary>
        [JsonProperty(PropertyName = "outputs")]
        public List<string>? Outputs { get; set; } = new List<string>();

        /// <summary>
        /// List of detected target types
        /// </summary>
        [JsonProperty(PropertyName = "targets")]
        public List<string> Targets { get; set; } = new List<string>();

        /// <summary>
        /// List of detected OS targets
        /// </summary>
        [JsonProperty(PropertyName = "OSTargets")]
        public List<string>? OSTargets { get; set; } = new List<string>();

        /// <summary>
        /// LIst of detected file types (extension based)
        /// </summary>
        [JsonProperty(PropertyName = "fileExtensions")]
        public List<string>? FileExtensions { get; set; } = new List<string>();

        /// <summary>
        /// List of detected cloud host targets
        /// </summary>
        [JsonProperty(PropertyName = "cloudTargets")]
        public List<string>? CloudTargets { get; set; } = new List<string>();

        /// <summary>
        /// List of detected cpu targets
        /// </summary>
        [JsonProperty(PropertyName = "CPUTargets")]
        public List<string>? CPUTargets { get; set; } = new List<string>();

        /// <summary>
        /// List of detected programming languages used and count of files 
        /// </summary>
        [JsonProperty(PropertyName = "languages")]
        public ImmutableSortedDictionary<string, int>? Languages { get; set; } //unable to init here for constr arg

        /// <summary>
        /// List of detected tag counters i.e. metrics
        /// </summary>
        [JsonProperty(PropertyName = "tagCounters")]
        public List<MetricTagCounter>? TagCounters { get; set; } = new List<MetricTagCounter>();

        /// <summary>
        /// List of detailed MatchRecords from scan
        /// </summary>
        [JsonProperty(PropertyName = "detailedMatchList")]
        public List<MatchRecord>? Matches { get; } = new List<MatchRecord>();

        public MetaData(string applicationName, string sourcePath)
        {
            ApplicationName = applicationName;
            SourcePath = sourcePath;
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