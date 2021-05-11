﻿// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Provides utilty help specific to aggregating metadata from analyze cmd matches while isolating scanned data from that process
    /// Hides complexity i.e. threaded scanning so that caller gets simple list<T> that is presorted and consistent each scan
    /// </summary>
    public class MetaDataHelper
    {
        //visible to callers i.e. AnalyzeCommand
        internal ConcurrentDictionary<string, byte> PackageTypes { get; set; } = new ConcurrentDictionary<string, byte>();
        internal ConcurrentDictionary<string, byte> FileExtensions { get; set; } = new ConcurrentDictionary<string, byte>();
        internal ConcurrentDictionary<string, byte> UniqueDependencies { get; set; } = new ConcurrentDictionary<string, byte>();

        private ConcurrentDictionary<string, byte> AppTypes { get; set; } = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<string, byte> FileNames { get; set; } = new ConcurrentDictionary<string, byte>();
        internal ConcurrentDictionary<string, byte> UniqueTags { get; set; } = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<string, byte> Outputs { get; set; } = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<string, byte> Targets { get; set; } = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<string, byte> CPUTargets { get; set; } = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<string, byte> CloudTargets { get; set; } = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<string, byte> OSTargets { get; set; } = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<string, MetricTagCounter> TagCounters { get; set; } = new ConcurrentDictionary<string, MetricTagCounter>();
        private ConcurrentDictionary<string, int> Languages { get; set; } = new ConcurrentDictionary<string, int>();

        internal ConcurrentBag<MatchRecord> Matches { get; set; } = new ConcurrentBag<MatchRecord>();
        internal ConcurrentBag<FileRecord> Files { get; set; } = new ConcurrentBag<FileRecord>();

        public int UniqueTagsCount { get { return UniqueTags.Keys.Count; } }

        internal MetaData Metadata { get; set; }

        public MetaDataHelper(string sourcePath, bool uniqueMatchesOnly)
        {
            sourcePath = Path.GetFullPath(sourcePath);//normalize for .\ and similar
            Metadata = new MetaData(GetDefaultProjectName(sourcePath), sourcePath);
        }

        /// <summary>
        /// Assist in aggregating reporting properties of matches as they are added
        /// Keeps helpers isolated from MetaData class which is used as a result object to keep pure
        /// </summary>
        /// <param name="matchRecord"></param>
        public void AddTagsFromMatchRecord(MatchRecord matchRecord)
        {
            //special handling for standard characteristics in report
            foreach (var tag in matchRecord.Tags ?? new string[] { })
            {
                switch (tag)
                {
                    case "Metadata.Application.Author":
                    case "Metadata.Application.Publisher":
                        Metadata.Authors = ExtractValue(matchRecord.Sample);
                        break;
                    case "Metadata.Application.Description":
                        Metadata.Description = ExtractValue(matchRecord.Sample);
                        break;
                    case "Metadata.Application.Name":
                        Metadata.ApplicationName = ExtractValue(matchRecord.Sample);
                        break;
                    case "Metadata.Application.Version":
                        Metadata.SourceVersion = ExtractValue(matchRecord.Sample);
                        break;
                    case "Metadata.Application.Target.Processor":
                        CPUTargets.TryAdd(ExtractValue(matchRecord.Sample).ToLower(), 0);
                        break;
                    case "Metadata.Application.Output.Type":
                        Outputs.TryAdd(ExtractValue(matchRecord.Sample).ToLower(), 0);
                        break;
                    case "Dependency.SourceInclude":
                        return; //design to keep noise out of detailed match list
                    default:
                        if (tag.Contains("Metric."))
                        {
                            _ = TagCounters.TryAdd(tag, new MetricTagCounter()
                            {
                                Tag = tag
                            });
                        }
                        else if (tag.Contains(".Platform.OS"))
                        {
                            OSTargets.TryAdd(tag.Substring(tag.LastIndexOf('.', tag.Length - 1) + 1), 0);
                        }
                        else if (tag.Contains("CloudServices.Hosting"))
                        {
                            CloudTargets.TryAdd(tag.Substring(tag.LastIndexOf('.', tag.Length - 1) + 1), 0);
                        }
                        break;
                }
            }

            //Special handling; attempt to detect app types...review for multiple pattern rule limitation
            string solutionType = DetectSolutionType(matchRecord);
            if (!string.IsNullOrEmpty(solutionType))
            {
                AppTypes.TryAdd(solutionType, 0);
            }

            bool CounterOnlyTagSet = false;
            var selected = matchRecord.Tags is not null ? TagCounters.Where(x => matchRecord.Tags.Any(y => y.Contains(x.Value.Tag ?? ""))) : new Dictionary<string, MetricTagCounter>();
            foreach (var select in selected)
            {
                CounterOnlyTagSet = true;
                select.Value.IncrementCount();
            }

            //omit adding if ther a counter metric tag
            if (!CounterOnlyTagSet)
            {
                //update list of unique tags as we go
                foreach (string tag in matchRecord.Tags ?? Array.Empty<string>())
                {
                    UniqueTags.TryAdd(tag, 0);
                }
            }
        }

        /// <summary>
        /// Assist in aggregating reporting properties of matches as they are added
        /// Keeps helpers isolated from MetaData class which is used as a result object to keep pure
        /// </summary>
        /// <param name="matchRecord"></param>
        public void AddMatchRecord(MatchRecord matchRecord)
        {
            //special handling for standard characteristics in report
            foreach (var tag in matchRecord.Tags ?? Array.Empty<string>())
            {
                switch (tag)
                {
                    case "Metadata.Application.Author":
                    case "Metadata.Application.Publisher":
                        Metadata.Authors = ExtractValue(matchRecord.Sample);
                        break;
                    case "Metadata.Application.Description":
                        Metadata.Description = ExtractValue(matchRecord.Sample);
                        break;
                    case "Metadata.Application.Name":
                        Metadata.ApplicationName = ExtractValue(matchRecord.Sample);
                        break;
                    case "Metadata.Application.Version":
                        Metadata.SourceVersion = ExtractValue(matchRecord.Sample);
                        break;
                    case "Metadata.Application.Target.Processor":
                        CPUTargets.TryAdd(ExtractValue(matchRecord.Sample).ToLower(), 0);
                        break;
                    case "Metadata.Application.Output.Type":
                        Outputs.TryAdd(ExtractValue(matchRecord.Sample).ToLower(), 0);
                        break;
                    case "Dependency.SourceInclude":
                        return; //design to keep noise out of detailed match list
                    default:
                        if (tag.Contains("Metric."))
                        {
                            TagCounters.TryAdd(tag, new MetricTagCounter()
                            {
                                Tag = tag
                            });
                            TagCounters[tag].IncrementCount();
                        }
                        else if (tag.Contains(".Platform.OS"))
                        {
                            OSTargets.TryAdd(tag.Substring(tag.LastIndexOf('.', tag.Length - 1) + 1), 0);
                        }
                        else if (tag.Contains("CloudServices.Hosting"))
                        {
                            CloudTargets.TryAdd(tag.Substring(tag.LastIndexOf('.', tag.Length - 1) + 1), 0);
                        }
                        break;
                }
            }

            //Special handling; attempt to detect app types...review for multiple pattern rule limitation
            string solutionType = DetectSolutionType(matchRecord);
            if (!string.IsNullOrEmpty(solutionType))
            {
                AppTypes.TryAdd(solutionType, 0);
            }

            var nonCounters = matchRecord.Tags?.Where(x => TagCounters.Any(y => y.Key == x)) ?? Array.Empty<string>();

            //omit adding if it if all the tags were counters
            if (nonCounters.Any())
            {
                //update list of unique tags as we go
                foreach (string tag in nonCounters)
                {
                    UniqueTags.TryAdd(tag, 0);
                }

                Matches.Add(matchRecord);
            }
        }


        /// <summary>
        /// Transfer concurrent data from scan to analyze result with sorted, simplier types for callers
        /// </summary>
        public void PrepareReport()
        {
            Metadata.CPUTargets = CPUTargets.Keys.ToList();
            Metadata.AppTypes = AppTypes.Keys.ToList();
            Metadata.OSTargets = OSTargets.Keys.ToList();
            Metadata.UniqueDependencies = UniqueDependencies.Keys.ToList();
            Metadata.UniqueTags = UniqueTags.Keys.ToList();
            Metadata.CloudTargets = CloudTargets.Keys.ToList();
            Metadata.PackageTypes = PackageTypes.Keys.ToList();
            Metadata.FileExtensions = FileExtensions.Keys.ToList();
            Metadata.Outputs = Outputs.Keys.ToList();
            Metadata.Targets = Targets.Keys.ToList();

            Metadata.Files = Files.ToList();
            Metadata.Matches = Matches.ToList();

            Metadata.CPUTargets.Sort();
            Metadata.AppTypes.Sort();
            Metadata.OSTargets.Sort();
            Metadata.UniqueDependencies.Sort();
            Metadata.UniqueTags.Sort();
            Metadata.CloudTargets.Sort();
            Metadata.PackageTypes.Sort();
            Metadata.FileExtensions.Sort();
            Metadata.Outputs.Sort();
            Metadata.Targets.Sort();

            Metadata.Languages = Languages.ToImmutableSortedDictionary();

            foreach (MetricTagCounter metricTagCounter in TagCounters.Values)
            {
                Metadata?.TagCounters?.Add(metricTagCounter);
            }
        }

        /// <summary>
        /// Defined here to isolate MetaData from data processing methods and keep as pure data
        /// </summary>
        /// <param name="language"></param>
        public void AddLanguage(string language)
        {
            Languages.AddOrUpdate(language, 1, (language, count) => count + 1);
        }

        #region helpers
        /// <summary>
        /// Initial best guess to deduce project name; if scanned metadata from project solution value is replaced later
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        private string GetDefaultProjectName(string sourcePath)
        {
            string applicationName = string.Empty;

            if (Directory.Exists(sourcePath))
            {
                if (sourcePath != string.Empty)
                {
                    if (sourcePath[^1] == Path.DirectorySeparatorChar) //in case path ends with dir separator; remove
                    {
                        applicationName = sourcePath.Trim(Path.DirectorySeparatorChar);
                    }
                    if (applicationName.LastIndexOf(Path.DirectorySeparatorChar) is int idx && idx != -1)
                    {
                        applicationName = applicationName[idx..].Trim();
                    }
                }
            }
            else
            {
                applicationName = Path.GetFileNameWithoutExtension(sourcePath);
            }

            return applicationName;
        }

        /// <summary>
        /// Attempt to map application type tags or file type or language to identify
        /// WebApplications, Windows Services, Client Apps, WebServices, Azure Functions etc.
        /// </summary>
        /// <param name="match"></param>
        public string DetectSolutionType(MatchRecord match)
        {
            string result = "";
            if (match.Tags is not null && match.Tags.Any(s => s.Contains("Application.Type")))
            {
                foreach (string tag in match.Tags ?? new string[] { })
                {
                    int index = tag.IndexOf("Application.Type");
                    if (-1 != index)
                    {
                        result = tag.Substring(index + 17);
                        break;
                    }
                }
            }
            else
            {
                switch (match.FileName)
                {
                    case "web.config":
                        result = "Web.Application";
                        break;

                    case "app.config":
                        result = ".NETclient";
                        break;

                    default:
                        switch (Path.GetExtension(match.FileName))
                        {
                            case ".cshtml":
                                result = "Web.Application";
                                break;

                            case ".htm":
                            case ".html":
                            case ".js":
                            case ".ts":
                                result = "Web.Application";
                                break;

                            case "powershell":
                            case "shellscript":
                            case "wincmdscript":
                                result = "script";
                                break;

                            default:
                                switch (match.Language)
                                {
                                    case "ruby":
                                    case "perl":
                                    case "php":
                                        result = "Web.Application";
                                        break;
                                }
                                break;
                        }
                        break;
                }
            }

            return result.ToLower();
        }

        private string ExtractValue(string s)
        {
            if (s.ToLower().Contains("</"))
            {
                return ExtractXMLValue(s);
            }
            else if (s.ToLower().Contains("<"))
            {
                return ExtractXMLValueMultiLine(s);
            }
            else if (s.ToLower().Contains(":"))
            {
                return ExtractJSONValue(s);
            }

            return s;
        }

        private static string ExtractJSONValue(string s)
        {
            var parts = s.Split(':');
            if (parts.Length == 2)
            {
                return parts[1].Replace("\"", "").Trim();
            }

            return s;
        }

        private string ExtractXMLValue(string s)
        {
            int firstTag = s.IndexOf(">");
            if (firstTag > -1)
            {
                int endTag = s.IndexOf("</", firstTag);
                if (endTag > -1)
                {
                    return s.Substring(firstTag + 1, endTag - firstTag - 1);
                }
            }

            return s;
        }

        private string ExtractXMLValueMultiLine(string s)
        {
            int firstTag = s.IndexOf(">");
            if (firstTag > -1 && firstTag < s.Length - 1)
            {
                return s[(firstTag + 1)..];
            }
            return s;
        }
    }

    #endregion
}