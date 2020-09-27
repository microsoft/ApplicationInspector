// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using DotLiquid;
using DotLiquid.FileSystems;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.CLI
{
    public class AnalyzeHtmlWriter : CommandResultsWriter
    {
        public Dictionary<string, List<TagInfo>> KeyedTagInfoLists { get; } = new Dictionary<string, List<TagInfo>>();

        public Dictionary<string, List<TagInfo>> KeyedSortedTagInfoLists { get; } = new Dictionary<string, List<TagInfo>>();

        public List<TagCategory>? TagGroupPreferences { get; set; }//read preferred list of groups and tags for profile / features page

        private MetaData? _appMetaData;
        private AnalyzeResult? _analyzeResult;

        public AnalyzeHtmlWriter()
        {
            KeyedTagInfoLists = new Dictionary<string, List<TagInfo>>();
            KeyedSortedTagInfoLists = new Dictionary<string, List<TagInfo>>();
        }

        /// <summary>
        /// Pre: AnalyzeCommand GetResults created and populated from RulesEngine
        /// </summary>
        /// <param name="result"></param>
        /// <param name="cLICommandOptions"></param>
        /// <param name="autoClose"></param>
        public override void WriteResults(Result result, CLICommandOptions cLICommandOptions, bool autoClose = true)
        {
            //recover metadata results from prior analyzecommand GetResults()
            _analyzeResult = (AnalyzeResult)result;
            _appMetaData = _analyzeResult.Metadata;

            PopulateTagGroups();
            WriteJsonResult();//dep link from html content
            WriteHtmlResult();
        }

        private void WriteHtmlResult()
        {
            RenderResultsSafeforHTML();

            //Grab any local css and js files that are needed i.e. don't have hosted URL's or are proprietary
            string allCSS = "<style>\n" + MergeResourceFiles(Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "html","resources","css"));
            string allJS = "<script type=\"text/javascript\">\n" + MergeResourceFiles(Path.Combine(Utils.GetPath(Utils.AppPath.basePath),"html","resources","js"));

            //Prepare html template merge
            string htmlTemplateText = File.ReadAllText(Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "html/index.html"));
            Template.FileSystem = new EmbeddedFileSystem(Assembly.GetEntryAssembly(), "Microsoft.ApplicationInspector.CLI.html.partials");

            //Update template with local aggregated code for easy relocation of output file
            htmlTemplateText = htmlTemplateText.Replace("<script type=\"text/javascript\">", allJS);
            htmlTemplateText = htmlTemplateText.Replace("<link rel=\"stylesheet\" type=\"text/css\" href=\"html/resources/css/appinspector.css\" />", allCSS+"</style>");

            RegisterSafeType(typeof(MetaData));

            //Prepare data for use in appinspector.js and html partials resources
            var htmlTemplate = Template.Parse(htmlTemplateText);
            var data = new Dictionary<string, object>();
            data["MetaData"] = _appMetaData ?? new MetaData("","");

            var hashData = new Hash();
            hashData["json"] = JsonConvert.SerializeObject(data);//json serialization required for [js] access to objects
            hashData["application_version"] = Utils.GetVersionString();

            //add dynamic sets of groups of taginfo read from preferences for Profile page
            List<TagGroup> tagGroupList = GetCategoryTagGroups("profile");
            hashData["groups"] = tagGroupList;

            //add summary values for sorted tags lists of taginfo
            foreach (string outerKey in KeyedSortedTagInfoLists.Keys)
            {
                hashData.Add(outerKey, KeyedSortedTagInfoLists[outerKey]);
            }

            hashData["cputargets"] = _appMetaData?.CPUTargets;
            hashData["apptypes"] = _appMetaData?.AppTypes ?? new List<string>();
            hashData["packagetypes"] = _appMetaData?.PackageTypes ?? new List<string>();
            hashData["ostargets"] = _appMetaData?.OSTargets ?? new List<string>();
            hashData["outputs"] = _appMetaData?.Outputs ?? new List<string>();
            hashData["filetypes"] = _appMetaData?.FileExtensions ?? new List<string>();
            hashData["tagcounters"] = ConvertTagCounters(_appMetaData?.TagCounters ?? new List<MetricTagCounter>());

            //final render and close
            var htmlResult = htmlTemplate.Render(hashData);
            TextWriter?.Write(htmlResult);
            FlushAndClose();
        }

        private void WriteJsonResult()
        {
            //writes out json report for convenient link from report summary page(s)
            CLIAnalyzeCmdOptions jsonOptions = new CLIAnalyzeCmdOptions()
            {
                OutputFileFormat = "json",
                OutputFilePath = "output.json"
            };

            //quiet normal write noise for json writter to just gen the file; then restore
            WriteOnce.ConsoleVerbosity saveVerbosity = WriteOnce.Verbosity;
            WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.None;
            CommandResultsWriter? resultsWriter = WriterFactory.GetWriter(jsonOptions);
            AnalyzeJsonWriter? jsonWriter = resultsWriter != null ? (AnalyzeJsonWriter)resultsWriter : null;
            if (_analyzeResult != null)
            {
                jsonWriter?.WriteResults(_analyzeResult, jsonOptions);
            }
            WriteOnce.Verbosity = saveVerbosity;
        }

        private void RegisterSafeType(Type type)
        {
            Template.RegisterSafeType(type, (t) => t.ToString());
            Template.RegisterSafeType(type, type.GetMembers(BindingFlags.Instance).Select((e) => e.Name).ToArray());
        }

        private string MergeResourceFiles(string inputPath)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (Directory.Exists(inputPath))
            {
                try
                {
                    IEnumerable<string> srcfileList = Directory.EnumerateFiles(inputPath, "*.*", SearchOption.AllDirectories);
                    if (!srcfileList.Any())
                    {
                        throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, inputPath));
                    }

                    foreach (string fileName in srcfileList)
                    {
                        stringBuilder.Append(string.Format("\n\n/*FILE: {0}*/\n\n", fileName));
                        stringBuilder.Append(File.ReadAllText(fileName));
                    }
                }
                catch (Exception)
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, inputPath));
                }
            }

            return stringBuilder.ToString();
        }

        #region UIAndReportResultsHelp

        /// <summary>
        /// Renders code i.e. user input safe for html display for default report
        /// Delayed html encoding allows original values to be rendered for the output form i.e.
        /// json, text formats, even nuget objects retain the originals for readability and 
        /// for managing output transformations as desired
        /// </summary>
        private void RenderResultsSafeforHTML()
        {
            //safeguard simple string meta-data
            if (_appMetaData?.ApplicationName != null)
                _appMetaData.ApplicationName = SafeString(_appMetaData?.ApplicationName);

            if (_appMetaData?.Description != null)
                _appMetaData.Description = SafeString(_appMetaData?.Description);

            if (_appMetaData?.Authors != null)
                _appMetaData.Authors = SafeString(_appMetaData?.Authors);

            if (_appMetaData?.SourceVersion != null)
                _appMetaData.SourceVersion = SafeString(_appMetaData?.SourceVersion);

            //safeguard lists data
            SafeList(_appMetaData?.AppTypes);
            SafeList(_appMetaData?.CloudTargets);
            SafeList(_appMetaData?.OSTargets);
            SafeList(_appMetaData?.Outputs);
            SafeList(_appMetaData?.PackageTypes);
            SafeList(_appMetaData?.Targets);
            SafeList(_appMetaData?.CPUTargets);

            //safeguard displayable fields in match records
            foreach (MatchRecord matchRecord in _appMetaData?.Matches ?? new List<MatchRecord>())
            {
                //safeguard sample output now that we've matched properties for blocking browser xss
                matchRecord.Sample = System.Net.WebUtility.HtmlEncode(matchRecord.Sample);
                matchRecord.Excerpt = System.Net.WebUtility.HtmlEncode(matchRecord.Excerpt);
            }
        }

        private void SafeList(List<string>? valuesList)
        {
            for (int i = 0; i < valuesList?.Count; i++)
            {
                valuesList[i] = SafeString(valuesList[i]);
            }
        }

        private string SafeString(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return System.Net.WebUtility.HtmlEncode(value);
            }

            return "";
        }

        /// <summary>
        /// Processing for organizing results into easy to use TagGroups for customizable display organization in HTML UI
        /// </summary>
        public void PopulateTagGroups()
        {
            //read default/user preferences on what tags to report presence on and groupings
            if (File.Exists(Utils.GetPath(Utils.AppPath.tagGroupPref)))
            {
                TagGroupPreferences = JsonConvert.DeserializeObject<List<TagCategory>>(File.ReadAllText(Utils.GetPath(Utils.AppPath.tagGroupPref)));
            }
            else
            {
                TagGroupPreferences = new List<TagCategory>();
            }

            string[] unSupportedGroupsOrPatterns = new string[] { "metric", "dependency" };

            //for each preferred group of tag patterns determine if at least one instance was detected
            foreach (TagCategory tagCategory in TagGroupPreferences)
            {
                foreach (TagGroup tagGroup in tagCategory.Groups ?? new List<TagGroup>())
                {
                    if (string.IsNullOrEmpty(tagGroup.Title))
                    {
                        WriteOnce.Log?.Warn($"Tag group with no title skipped");
                        continue;
                    }

                    bool test = tagGroup.Title.ToLower().Contains(unSupportedGroupsOrPatterns[0]);
                    if (unSupportedGroupsOrPatterns.Any(x => tagGroup.Title.ToLower().Contains(x)))
                    {
                        WriteOnce.Log?.Warn($"Unsupported tag group or pattern detected '{tagGroup.Title}'.  See online documentation at https://github.com/microsoft/ApplicationInspector/wiki/3.5-Tags");
                    }

                    foreach (TagSearchPattern pattern in tagGroup.Patterns ?? new List<TagSearchPattern>())
                    {
                        pattern.Detected = _appMetaData != null && _appMetaData.UniqueTags.Any(v => v.Contains(pattern.SearchPattern));
                        if (unSupportedGroupsOrPatterns.Any(x => pattern.SearchPattern.ToLower().Contains(x)))
                        {
                            WriteOnce.Log?.Warn($"Unsupported tag group or pattern detected '{pattern.SearchPattern}'.  See online documentation at https://github.com/microsoft/ApplicationInspector/wiki/3.5-Tags"); 
                        }

                        //create dynamic "category" groups of tags with pattern relationship established from TagReportGroups.json
                        //that can be used to populate reports with various attributes for each tag detected
                        if (pattern.Detected)
                        {
                            bool uniqueTagsOnly = tagCategory.Type == TagCategory.tagInfoType.uniqueTags;
                            KeyedTagInfoLists["tagGrp" + tagGroup.DataRef] = GetTagInfoListByTagGroup(tagGroup, uniqueTagsOnly);
                        }
                    }
                }
            }

            //create simple ranked page lists HTML use
            KeyedSortedTagInfoLists["tagGrpAllTagsByConfidence"] = GetTagInfoListByConfidence();
            KeyedSortedTagInfoLists["tagGrpAllTagsBySeverity"] = GetTagInfoListBySeverity();
            KeyedSortedTagInfoLists["tagGrpAllTagsByName"] = GetTagInfoListByName();
        }

        /// <summary>
        /// Get a list of TagGroup for a given category section name e.g. profile
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public List<TagGroup> GetCategoryTagGroups(string category)
        {
            List<TagGroup> result = new List<TagGroup>();
            //get all tag groups for specified category
            foreach (TagCategory categoryTagGroup in TagGroupPreferences ?? new List<TagCategory>())
            {
                if (categoryTagGroup.Name == category)
                {
                    result = categoryTagGroup.Groups ?? new List<TagGroup>();
                    break;
                }
            }

            //now get all matches for that group i.e. Authentication
            foreach (TagGroup group in result)
            {
                GetTagInfoListByTagGroup(group);
            }

            return result;
        }

        /// <summary>
        /// MetaData.UniqueTags should already exists and be created incrementally but here in case
        /// </summary>
        /// <returns></returns>
        private HashSet<string> GetUniqueTags()
        {
            HashSet<string> results = new HashSet<string>();

            foreach (MatchRecord match in _appMetaData?.Matches ?? new List<MatchRecord>())
            {
                foreach (string tag in match.Tags ?? new string[] { })
                {
                    results.Add(tag);
                }
            }

            return results;
        }

        /// <summary>
        /// Builds list of matching tags by profile pattern
        /// Ensures only one instance of a given tag in results unlike GetAllMatchingTags method
        /// with highest confidence level for that tag pattern
        /// </summary>
        /// <param name="tagPattern"></param>
        /// <returns></returns>
        private List<TagInfo> GetTagInfoListByTagGroup(TagGroup tagGroup, bool addNotFound = true)
        {
            List<TagInfo> result = new List<TagInfo>();
            HashSet<string> hashSet = new HashSet<string>();

            foreach (TagSearchPattern pattern in tagGroup.Patterns ?? new List<TagSearchPattern>())
            {
                if (pattern.Detected)//set at program.RollUp already so don't search for again
                {
                    var tagPatternRegex = pattern.Expression;

                    foreach (var match in _appMetaData?.Matches ?? new List<MatchRecord>())
                    {
                        foreach (var tagItem in match.Tags ?? new string[] { })
                        {
                            if (tagPatternRegex.IsMatch(tagItem))
                            {
                                if (!hashSet.Contains(pattern.SearchPattern))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = tagItem,
                                        Confidence = match.Confidence.ToString(),
                                        Severity = match.Severity.ToString(),
                                        ShortTag = pattern.DisplayName,
                                        StatusIcon = pattern.DetectedIcon,
                                        Detected = true
                                    });

                                    hashSet.Add(pattern.SearchPattern);

                                    pattern.Confidence = match.Confidence.ToString();
                                }
                                else
                                {
                                    //ensure we get highest confidence, severity as there are likely multiple matches for this tag pattern
                                    foreach (TagInfo updateItem in result)
                                    {
                                        if (updateItem.Tag == tagItem)
                                        {
                                            Confidence oldConfidence;
                                            Enum.TryParse(updateItem.Confidence, out oldConfidence);

                                            if (match.Confidence > oldConfidence)
                                            {
                                                updateItem.Confidence = match.Confidence.ToString();
                                                pattern.Confidence = match.Confidence.ToString();
                                            }

                                            Severity oldSeverity;
                                            Enum.TryParse(updateItem.Severity, out oldSeverity);
                                            if (match.Severity > oldSeverity)
                                            {
                                                updateItem.Severity = match.Severity.ToString();
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (addNotFound) //allow to report on false presense items
                {
                    TagInfo tagInfo = new TagInfo
                    {
                        Tag = pattern.SearchPattern,
                        Detected = false,
                        ShortTag = pattern.DisplayName,
                        StatusIcon = pattern.NotDetectedIcon,
                        Confidence = "",
                        Severity = ""
                    };

                    pattern.Confidence = "";
                    result.Add(tagInfo);
                    hashSet.Add(tagInfo.Tag);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a set of matching tags for a set of patterns, returning for all matches
        /// </summary>
        /// <param name="patterns"></param>
        /// <param name="addNotFound"></param>
        /// <returns></returns>
        private List<TagInfo> GetAllMatchingTagInfoList(TagGroup tagGroup, bool addNotFound = true)
        {
            List<TagInfo> result = new List<TagInfo>();
            HashSet<string> hashSet = new HashSet<string>();

            foreach (TagSearchPattern pattern in tagGroup.Patterns ?? new List<TagSearchPattern>())
            {
                if (pattern.Detected)
                {
                    var tagPatternRegex = pattern.Expression;

                    foreach (var match in _appMetaData?.Matches ?? new List<MatchRecord>())
                    {
                        foreach (var tagItem in match.Tags ?? new string[] { })
                        {
                            if (tagPatternRegex.IsMatch(tagItem))
                            {
                                if (!hashSet.Contains(tagItem))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = tagItem,
                                        Confidence = match.Confidence.ToString(),
                                        Severity = match.Severity.ToString(),
                                        ShortTag = tagItem.Substring(tagItem.LastIndexOf('.') + 1),
                                        StatusIcon = pattern.DetectedIcon,
                                        Detected = true
                                    });

                                    hashSet.Add(tagItem);
                                }
                                else
                                { //ensure we have highest confidence, severity as there are likly multiple matches for this tag pattern
                                    foreach (TagInfo updateItem in result)
                                    {
                                        if (updateItem.Tag == tagItem)
                                        {
                                            Confidence oldConfidence;
                                            Enum.TryParse(updateItem.Confidence, out oldConfidence);

                                            if (match.Confidence > oldConfidence)
                                            {
                                                updateItem.Confidence = match.Confidence.ToString();
                                                pattern.Confidence = match.Confidence.ToString();
                                            }

                                            Severity oldSeverity;
                                            Enum.TryParse(updateItem.Severity, out oldSeverity);
                                            if (match.Severity > oldSeverity)
                                            {
                                                updateItem.Severity = match.Severity.ToString();
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// List of taginfo items ordered by name
        /// </summary>
        /// <returns></returns>
        private List<TagInfo> GetTagInfoListByName()
        {
            HashSet<string> dupCheck = new HashSet<string>();
            List<TagInfo> result = new List<TagInfo>();

            foreach (string tag in _appMetaData?.UniqueTags ?? new List<string>())
            {
                foreach (var match in _appMetaData?.Matches ?? new List<MatchRecord>())
                {
                    foreach (string testTag in match.Tags ?? new string[] { })
                    {
                        if (tag == testTag)
                        {
                            if (dupCheck.Add(testTag))
                            {
                                result.Add(new TagInfo
                                {
                                    Tag = testTag,
                                    Confidence = match.Confidence.ToString(),
                                    Severity = match.Severity.ToString(),
                                    ShortTag = testTag.Substring(testTag.LastIndexOf('.') + 1),
                                });

                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Tags sorted by confidence
        /// </summary>
        /// <returns></returns>
        private List<TagInfo> GetTagInfoListByConfidence()
        {
            List<TagInfo> result = new List<TagInfo>();
            HashSet<string> dupCheck = new HashSet<string>();
            RulesEngine.Confidence[] confidences = { Confidence.High, Confidence.Medium, Confidence.Low };

            foreach (string tag in _appMetaData?.UniqueTags?? new List<string>())
            {
                var searchPattern = new Regex(tag, RegexOptions.IgnoreCase);
                foreach (Confidence confidence in confidences)
                {
                    foreach (var match in _appMetaData?.Matches ?? new List<MatchRecord>())
                    {
                        foreach (string testTag in match.Tags ?? new string[] { })
                        {
                            if (searchPattern.IsMatch(testTag))
                            {
                                if (match.Confidence == confidence && dupCheck.Add(tag))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = testTag,
                                        Confidence = confidence.ToString(),
                                        Severity = match.Severity.ToString(),
                                        ShortTag = testTag.Substring(testTag.LastIndexOf('.') + 1),
                                    });
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Sorted by Severity
        /// </summary>
        /// <returns></returns>
        private List<TagInfo> GetTagInfoListBySeverity()
        {
            List<TagInfo> result = new List<TagInfo>();
            HashSet<string> dupCheck = new HashSet<string>();
            Severity[] severities = { Severity.Critical, Severity.Important, Severity.Moderate, Severity.BestPractice, Severity.ManualReview };

            foreach (string tag in _appMetaData?.UniqueTags ?? new List<string>())
            {
                var searchPattern = new Regex(tag, RegexOptions.IgnoreCase);
                foreach (Severity severity in severities)
                {
                    foreach (var match in _appMetaData?.Matches ?? new List<MatchRecord>())
                    {
                        foreach (string testTag in match.Tags ?? new string[] { })
                        {
                            if (searchPattern.IsMatch(testTag))
                            {
                                if (match.Severity == severity && dupCheck.Add(tag))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = testTag,
                                        Confidence = match.Confidence.ToString(),
                                        Severity = severity.ToString(),
                                        ShortTag = testTag.Substring(testTag.LastIndexOf('.') + 1),
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Opportunity for any final data prep before report gen
        /// </summary>
        public List<TagCounterUI> ConvertTagCounters(IEnumerable<MetricTagCounter> metricTagCounters)
        {
            List<TagCounterUI> result = new List<TagCounterUI>();
            //TagCountersUI is liquid compatible while TagCounters is not to support json serialization; the split prevents exception
            //not fixable via json iteration disabling

            result.AddRange(metricTagCounters.Select(counter => new TagCounterUI()
            {
                Tag = counter.Tag,
                Count = counter.Count
            }));

            return result;
        }

        #endregion UIAndReportResultsHelp
    }

    /// <summary>
    /// Compatible for liquid; used to avoid use in MetaData as it adds unwanted properties to json serialization
    /// </summary>
    public class TagCounterUI : Drop
    {
        [JsonProperty(PropertyName = "tag")]
        public string? Tag { get; set; }

        [JsonProperty(PropertyName = "displayName")]
        public string? ShortTag { get; set; }

        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }

        [JsonProperty(PropertyName = "includeAsMatch")]
        public bool IncludeAsMatch => false;
    }
}
