// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Provides utilty help specific to aggregating metadata from analyze cmd matches while isolating scanned data from that process
    /// </summary>
    public class MetaDataHelper
    {
        //inhouse common properties to capture
        private Dictionary<string, string> _propertyTagSearchPatterns;

        public MetaData Metadata { get; set; }

        public MetaDataHelper(string sourcePath, bool uniqueMatchesOnly)
        {
            Metadata = new MetaData(GetDefaultProjectName(sourcePath), sourcePath);

            _propertyTagSearchPatterns = new Dictionary<string, string>();
            _propertyTagSearchPatterns.Add("strGrpOSTargets", ".OS.Targets");
            _propertyTagSearchPatterns.Add("strGrpCloudTargets", ".Cloud");
            _propertyTagSearchPatterns.Add("strGrpOutputs", ".Outputs");
            _propertyTagSearchPatterns.Add("strGrpCPUTargets", ".CPU");
        }


        /// <summary>
        /// Assist in aggregating reporting properties of matches as they are added 
        /// Keeps helpers isolated from MetaData class which is used as a result object to keep pure
        /// </summary>
        /// <param name="matchRecord"></param>
        public void AddMatchRecord(MatchRecord matchRecord)
        {
            //aggregate lists of matches against standard set of properties to report on
            foreach (string key in _propertyTagSearchPatterns.Keys)
            {
                var tagPatternRegex = new Regex(_propertyTagSearchPatterns[key], RegexOptions.IgnoreCase);
                if (matchRecord.Tags.Any(v => tagPatternRegex.IsMatch(v)))
                {
                    Metadata.KeyedPropertyLists[key].Add(matchRecord.Sample);
                }
            }

            // single standard properties we capture from any supported file type; others just captured as general tag matches...
            if (matchRecord.Tags.Any(v => v.Contains("Metadata.Application.Author")))
                Metadata.Authors = ExtractValue(matchRecord.Sample);
            if (matchRecord.Tags.Any(v => v.Contains("Metadata.Application.Publisher")))
                Metadata.Authors = ExtractValue(matchRecord.Sample);
            if (matchRecord.Tags.Any(v => v.Contains("Metadata.Application.Description")))
                Metadata.Description = ExtractValue(matchRecord.Sample);
            if (matchRecord.Tags.Any(v => v.Contains("Metadata.Application.Name")))
                Metadata.ApplicationName = ExtractValue(matchRecord.Sample);
            if (matchRecord.Tags.Any(v => v.Contains("Metadata.Application.Version")))
                Metadata.SourceVersion = ExtractValue(matchRecord.Sample);
            if (matchRecord.Tags.Any(v => v.Contains("Metadata.Application.Target.Processor")))
                Metadata.CPUTargets.Add(ExtractValue(matchRecord.Sample).ToLower());
            if (matchRecord.Tags.Any(v => v.Contains("Metadata.Application.Output.Type")))
                Metadata.Outputs.Add(ExtractValue(matchRecord.Sample).ToLower());
            if (matchRecord.Tags.Any(v => v.Contains("Platform.OS")))
                Metadata.OSTargets.Add(ExtractValue(matchRecord.Sample).ToLower());
            if (matchRecord.Tags.Any(v => v.Contains("Metric."))) Metadata.TagCounters.Add(new MetricTagCounter()
            {
                Tag = matchRecord.Tags[0],
                Count = 0
            });

            //safeguard sample output blocking browser xss
            matchRecord.Sample = System.Net.WebUtility.HtmlEncode(matchRecord.Sample);

            //Special handling; attempt to detect app types...review for multiple pattern rule limitation
            String solutionType = DetectSolutionType(matchRecord);
            if (!string.IsNullOrEmpty(solutionType))
                Metadata.AppTypes.Add(solutionType);

            //Update metric counters for default or user specified tags; don't add as match detail
            bool counterOnlyTag = false;
            foreach (MetricTagCounter counter in Metadata.TagCounters)
            {
                if (matchRecord.Tags.Any(v => v.Contains(counter.Tag)))
                {
                    counterOnlyTag = true;
                    counter.Count++;
                    break;
                }
            }

            //omit adding if only a counter metric tag
            if (!counterOnlyTag)
            {
                //update list of unique tags as we go
                foreach (string tag in matchRecord.Tags)
                    Metadata.UniqueTags.Add(tag);

                Metadata.MatchList.Add(matchRecord);
            }
            else
            {
                Metadata.TotalMatchesCount -= 1;//reduce e.g. tag counters only as per preferences file
            }

        }


        /// <summary>
        /// Defined here to isolate MetaData from data processing methods and keep as pure data
        /// </summary>
        /// <param name="language"></param>
        public void AddLanguage(string language)
        {
            if (Metadata.Languages.ContainsKey(language))
                Metadata.Languages[language]++;
            else
                Metadata.Languages.Add(language, 1);

        }



        string GetDefaultProjectName(string sourcePath)
        {
            string applicationName = "";

            //test effort to deduce project name
            try
            {
                if (Directory.Exists(sourcePath))
                {
                    try
                    {
                        applicationName = sourcePath.Substring(sourcePath.LastIndexOf(Path.DirectorySeparatorChar)).Replace(Path.DirectorySeparatorChar, ' ').Trim();
                    }
                    catch (Exception)
                    {
                        applicationName = Path.GetFileNameWithoutExtension(sourcePath);
                    }
                }
                else
                {
                    applicationName = Path.GetFileNameWithoutExtension(sourcePath);
                }
            }
            catch (Exception)
            {

            }

            return applicationName;
        }


        /// <summary>
        /// Attempt to map application type tags or file type or language to identify
        /// WebApplications, Windows Services, Client Apps, WebServices, Azure Functions etc.
        /// </summary>
        /// <param name="match"></param>
        public String DetectSolutionType(MatchRecord match)
        {
            string result = "";
            if (match.Tags.Any(s => s.Contains("Application.Type")))
            {
                foreach (string tag in match.Tags)
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
                                switch (match.Language.Name)
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
                return ExtractXMLValue(s);
            else
                return ExtractJSONValue(s);
        }

        private static string ExtractJSONValue(string s)
        {
            string result = "";
            try
            {
                var parts = s.Split(':');
                var value = parts[1];
                value = value.Replace("\"", "");
                result = value.Trim();

            }
            catch (Exception)
            {
                result = s;
            }

            return System.Net.WebUtility.HtmlEncode(result);
        }


        private string ExtractXMLValue(string s)
        {
            string result = "";
            try
            {
                int firstTag = s.IndexOf(">");
                int endTag = s.IndexOf("</", firstTag);
                var value = s.Substring(firstTag + 1, endTag - firstTag - 1);
                result = value;
            }
            catch (Exception)
            {
                result = s;
            }

            return System.Net.WebUtility.HtmlEncode(result);
        }


    }

}