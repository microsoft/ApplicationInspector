// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.Commands;

/// <summary>
///     Contains the analyze scanned meta data elements including rollup data for reporting purposes
/// </summary>
public class MetaData
{
    public MetaData(string applicationName, string sourcePath)
    {
        ApplicationName = applicationName;
        SourcePath = sourcePath;
    }

    //simple properties
    /// <summary>
    ///     Detected or derived project name
    /// </summary>
    [JsonPropertyName("applicationName")]
    public string? ApplicationName { get; set; }

    /// <summary>
    ///     Source path provided argument
    /// </summary>
    [JsonPropertyName("sourcePath")]
    public string? SourcePath { get; set; }

    /// <summary>
    ///     Detected project source version
    /// </summary>
    [JsonPropertyName("sourceVersion")]
    public string? SourceVersion { get; set; }

    /// <summary>
    ///     Detected source authors
    /// </summary>
    [JsonPropertyName("authors")]
    public string? Authors { get; set; }

    /// <summary>
    ///     Detected source description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    ///     Last modified date for source code scanned
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public string LastUpdated
    {
        get
        {
            if (Files.Any())
            {
                return Files.Select(x => x.ModifyTime).Max().ToString();
            }

            return DateTime.MinValue.ToString();
        }
    }

    /// <summary>
    ///     Date of analyze scan
    /// </summary>
    [JsonPropertyName("dateScanned")]
    public string? DateScanned { get; set; }


    /// <summary>
    ///     True if the overall analysis timed out
    /// </summary>
    [JsonPropertyName("timedOut")]
    public bool TimedOut { get; set; }

    //stats
    /// <summary>
    ///     Total number of files in source path
    /// </summary>
    [JsonPropertyName("totalFiles")]
    public int TotalFiles => Files.Count;

    /// <summary>
    ///     Total number of files Timed out on an individual timeout
    /// </summary>
    [JsonPropertyName("filesTimedOut")]
    public int FilesTimedOut
    {
        get { return Files.Count(x => x.Status == ScanState.TimedOut); }
    }

    /// <summary>
    ///     Total number of files scanned
    /// </summary>
    [JsonPropertyName("filesAnalyzed")]
    public int FilesAnalyzed
    {
        get { return Files.Count(x => x.Status is ScanState.Analyzed or ScanState.Affected); }
    }

    /// <summary>
    ///     Total number of skipped files based on supported formats
    /// </summary>
    [JsonPropertyName("filesSkipped")]
    public int FilesSkipped
    {
        get { return Files.Count(x => x.Status == ScanState.Skipped); }
    }

    /// <summary>
    ///     Total number of skipped files based on overall timeout
    /// </summary>
    [JsonPropertyName("filesTimeOutSkipped")]
    public int FilesTimeOutSkipped
    {
        get { return Files.Count(x => x.Status == ScanState.TimeOutSkipped); }
    }

    /// <summary>
    ///     Total files with at least one result
    /// </summary>
    [JsonPropertyName("filesAffected")]
    public int FilesAffected
    {
        get { return Files.Count(x => x.Status == ScanState.Affected); }
    }

    /// <summary>
    ///     Number of files which encountered an error when processing other than timing out.
    /// </summary>
    [JsonPropertyName("filesErrored")]
    public int FileErrored
    {
        get { return Files.Count(x => x.Status == ScanState.Error); }
    }

    /// <summary>
    ///     Total matches with supplied argument settings
    /// </summary>
    [JsonPropertyName("totalMatchesCount")]
    public int TotalMatchesCount => Matches?.Count ?? 0;

    /// <summary>
    ///     Total unique matches by Rule Id
    /// </summary>
    [JsonPropertyName("uniqueMatchesCount")]
    public int UniqueMatchesCount
    {
        get { return Matches?.Select(x => x.RuleId).Distinct().Count() ?? 0; }
    }

    /// <summary>
    ///     List of detected package types
    /// </summary>
    [JsonPropertyName("packageTypes")]
    public List<string>? PackageTypes { get; set; } = new();

    /// <summary>
    ///     List of detected application types
    /// </summary>
    [JsonPropertyName("appTypes")]
    public List<string>? AppTypes { get; set; } = new();

    /// <summary>
    ///     List of detected unique tags
    /// </summary>
    [JsonPropertyName("uniqueTags")]
    public List<string> UniqueTags { get; set; } = new();

    /// <summary>
    ///     List of detected unique code dependency includes
    /// </summary>
    [JsonPropertyName("uniqueDependencies")]
    public List<string>? UniqueDependencies { get; set; } = new();

    /// <summary>
    ///     List of detected output types
    /// </summary>
    [JsonPropertyName("outputs")]
    public List<string>? Outputs { get; set; } = new();

    /// <summary>
    ///     List of detected target types
    /// </summary>
    [JsonPropertyName("targets")]
    public List<string> Targets { get; set; } = new();

    /// <summary>
    ///     List of detected OS targets
    /// </summary>
    [JsonPropertyName("OSTargets")]
    public List<string>? OSTargets { get; set; } = new();

    /// <summary>
    ///     LIst of detected file types (extension based)
    /// </summary>
    [JsonPropertyName("fileExtensions")]
    public List<string>? FileExtensions { get; set; } = new();

    /// <summary>
    ///     List of detected cloud host targets
    /// </summary>
    [JsonPropertyName("cloudTargets")]
    public List<string>? CloudTargets { get; set; } = new();

    /// <summary>
    ///     List of detected cpu targets
    /// </summary>
    [JsonPropertyName("CPUTargets")]
    public List<string>? CPUTargets { get; set; } = new();

    /// <summary>
    ///     List of detected programming languages used and count of files
    /// </summary>
    [JsonPropertyName("languages")]
    public IDictionary<string, int>? Languages { get; set; } //unable to init here for constr arg

    /// <summary>
    ///     List of detected tag counters i.e. metrics
    /// </summary>
    [JsonPropertyName("tagCounters")]
    public List<MetricTagCounter>? TagCounters { get; set; } = new();

    /// <summary>
    ///     List of detailed MatchRecords from scan
    /// </summary>
    [JsonPropertyName("detailedMatchList")]
    public List<MatchRecord> Matches { get; set; } = new();

    [JsonPropertyName("filesInformation")]
    public List<FileRecord> Files { get; set; } = new();
}