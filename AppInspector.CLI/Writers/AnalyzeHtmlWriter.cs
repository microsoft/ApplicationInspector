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
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.CLI
{
    public class AnalyzeHtmlWriter : CommandResultsWriter
    {
        public Dictionary<string, List<TagInfo>> KeyedTagInfoLists { get; }//dynamic lists for grouping tag properties in reporting

        public Dictionary<string, List<TagInfo>> KeyedSortedTagInfoLists { get; } //split to avoid json serialization with others

        public List<TagCategory> TagGroupPreferences { get; set; }//read preferred list of groups and tags for profile page

        private MetaData _appMetaData;
        private AnalyzeResult _analyzeResult;

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
            _appMetaData = _analyzeResult.MetaData;

            PopulateTagGroups();
            WriteJsonResult();//dep link from html content
            WriteHtmlResult();
        }

        private void WriteHtmlResult()
        {
            //Prepare html template merge
            var htmlTemplateText = File.ReadAllText(Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "html/index.html"));
            Template.FileSystem = new EmbeddedFileSystem(Assembly.GetEntryAssembly(), "Microsoft.ApplicationInspector.CLI.html.partials");
            RegisterSafeType(typeof(MetaData));

            //Prepare data for use in appinspector.js and html partials resources
            var htmlTemplate = Template.Parse(htmlTemplateText);
            var data = new Dictionary<string, object>();
            data["MetaData"] = _appMetaData;

            var hashData = new Hash();
            hashData["json"] = Newtonsoft.Json.JsonConvert.SerializeObject(data);//json serialization required for [js] access to objects
            hashData["application_version"] = Utils.GetVersionString();

            //add dynamic sets of groups of taginfo read from preferences for Profile page
            List<TagGroup> tagGroupList = GetCategoryTagGroups("profile");
            hashData["groups"] = tagGroupList;

            //add summary values for sorted tags lists of taginfo
            foreach (string outerKey in KeyedSortedTagInfoLists.Keys)
            {
                hashData.Add(outerKey, KeyedSortedTagInfoLists[outerKey]);
            }

            //add summary metadata lists TODO remove all these as we already passed metadata obj
            hashData["cputargets"] = _appMetaData.CPUTargets;
            hashData["apptypes"] = _appMetaData.AppTypes;
            hashData["packagetypes"] = _appMetaData.PackageTypes;
            hashData["ostargets"] = _appMetaData.OSTargets;
            hashData["outputs"] = _appMetaData.Outputs;
            hashData["filetypes"] = _appMetaData.FileExtensions;
            hashData["tagcounters"] = ConvertTagCounters(_appMetaData.TagCounters);

            //final render and close
            var htmlResult = htmlTemplate.Render(hashData);
            string htmlOutputFilePath = Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "output.html");
            TextWriter.Write(htmlResult);
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
            AnalyzeJsonWriter jsonWriter = (AnalyzeJsonWriter)WriterFactory.GetWriter(jsonOptions);
            jsonWriter.WriteResults(_analyzeResult, jsonOptions);
            jsonWriter = null;
            WriteOnce.Verbosity = saveVerbosity;
        }



        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
            TextWriter = null;
        }

        private void RegisterSafeType(Type type)
        {
            Template.RegisterSafeType(type, (t) => t.ToString());
            Template.RegisterSafeType(type, type.GetMembers(BindingFlags.Instance).Select((e) => e.Name).ToArray());
        }



        #region UIAndReportResultsHelp

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

            //for each preferred group of tag patterns determine if at least one instance was detected
            foreach (TagCategory tagCategory in TagGroupPreferences)
            {
                foreach (TagGroup tagGroup in tagCategory.Groups)
                {
                    foreach (TagSearchPattern pattern in tagGroup.Patterns)
                    {
                        pattern.Detected = _appMetaData.UniqueTags.Any(v => v.Contains(pattern.SearchPattern));
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
            foreach (TagCategory categoryTagGroup in TagGroupPreferences)
            {
                if (categoryTagGroup.Name == category)
                {
                    result = categoryTagGroup.Groups;
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

            foreach (MatchRecord match in _appMetaData.MatchList)
            {
                foreach (string tag in match.Tags)
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

            foreach (TagSearchPattern pattern in tagGroup.Patterns)
            {
                if (pattern.Detected)//set at program.RollUp already so don't search for again
                {
                    var tagPatternRegex = new Regex(pattern.SearchPattern, RegexOptions.IgnoreCase);

                    foreach (var match in _appMetaData.MatchList)
                    {
                        foreach (var tagItem in match.Tags)
                        {
                            if (tagPatternRegex.IsMatch(tagItem))
                            {
                                if (!hashSet.Contains(pattern.SearchPattern))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = tagItem,
                                        Confidence = match.PatternConfidence,
                                        Severity = match.Severity,
                                        ShortTag = pattern.DisplayName,
                                        StatusIcon = pattern.DetectedIcon,
                                        Detected = true
                                    });

                                    hashSet.Add(pattern.SearchPattern);

                                    pattern.Confidence = match.PatternConfidence;

                                }
                                else
                                {
                                    //ensure we get highest confidence, severity as there are likely multiple matches for this tag pattern
                                    foreach (TagInfo updateItem in result)
                                    {
                                        if (updateItem.Tag == tagItem)
                                        {
                                            Confidence oldConfidence, newConfidence;
                                            Enum.TryParse(updateItem.Confidence, out oldConfidence);
                                            Enum.TryParse(match.PatternConfidence, out newConfidence);

                                            if (newConfidence > oldConfidence)
                                            {
                                                updateItem.Confidence = match.PatternConfidence;
                                                pattern.Confidence = match.PatternConfidence;
                                            }

                                            Severity oldSeverity, newtSeverity;
                                            Enum.TryParse(updateItem.Severity, out oldSeverity);
                                            Enum.TryParse(match.Severity, out newtSeverity);
                                            if (newtSeverity > oldSeverity)
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

            foreach (TagSearchPattern pattern in tagGroup.Patterns)
            {
                if (pattern.Detected)
                {
                    var tagPatternRegex = new Regex(pattern.SearchPattern, RegexOptions.IgnoreCase);

                    foreach (var match in _appMetaData.MatchList)
                    {
                        foreach (var tagItem in match.Tags)
                        {
                            if (tagPatternRegex.IsMatch(tagItem))
                            {
                                if (!hashSet.Contains(tagItem))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = tagItem,
                                        Confidence = match.PatternConfidence,
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
                                            Confidence oldConfidence, newConfidence;
                                            Enum.TryParse(updateItem.Confidence, out oldConfidence);
                                            Enum.TryParse(match.PatternConfidence, out newConfidence);

                                            if (newConfidence > oldConfidence)
                                            {
                                                updateItem.Confidence = match.PatternConfidence;
                                                pattern.Confidence = match.PatternConfidence;
                                            }

                                            Severity oldSeverity, newtSeverity;
                                            Enum.TryParse(updateItem.Severity, out oldSeverity);
                                            Enum.TryParse(match.Severity, out newtSeverity);
                                            if (newtSeverity > oldSeverity)
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
            List<string> orderedTags = _appMetaData.UniqueTags.ToList<string>();
            orderedTags.Sort();
            HashSet<string> dupCheck = new HashSet<string>();
            List<TagInfo> result = new List<TagInfo>();

            foreach (string tag in orderedTags)
            {
                foreach (var match in _appMetaData.MatchList)
                {
                    foreach (string testTag in match.Tags)
                    {
                        if (tag == testTag)
                        {
                            if (dupCheck.Add(testTag))
                            {
                                result.Add(new TagInfo
                                {
                                    Tag = testTag,
                                    Confidence = match.PatternConfidence,
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
        /// Todo: address array of tags in rule
        /// </summary>
        /// <returns></returns>
        private List<TagInfo> GetTagInfoListByConfidence()
        {
            List<TagInfo> result = new List<TagInfo>();
            HashSet<string> dupCheck = new HashSet<string>();
            RulesEngine.Confidence[] confidences = { Confidence.High, Confidence.Medium, Confidence.Low };

            foreach (Confidence test in confidences)
            {
                foreach (string tag in _appMetaData.UniqueTags)
                {
                    var searchPattern = new Regex(tag, RegexOptions.IgnoreCase);
                    foreach (var match in _appMetaData.MatchList)
                    {
                        foreach (string testTag in match.Tags)
                        {
                            if (searchPattern.IsMatch(testTag))
                            {
                                Confidence matchConfidence;
                                Enum.TryParse(match.PatternConfidence, out matchConfidence);

                                if (matchConfidence == test && dupCheck.Add(tag))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = testTag,
                                        Confidence = test.ToString(),
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
            RulesEngine.Severity[] severities = { Severity.Critical, Severity.Important, Severity.Moderate, Severity.BestPractice, Severity.ManualReview };

            foreach (Severity test in severities)
            {
                foreach (string tag in _appMetaData.UniqueTags)
                {
                    var searchPattern = new Regex(tag, RegexOptions.IgnoreCase);
                    foreach (var match in _appMetaData.MatchList)
                    {
                        foreach (string testTag in match.Tags)
                        {
                            if (searchPattern.IsMatch(testTag))
                            {
                                Severity matchSeverity;
                                Enum.TryParse(match.Severity, out matchSeverity);

                                if (matchSeverity == test && dupCheck.Add(tag))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = testTag,
                                        Confidence = match.Severity,
                                        Severity = test.ToString(),
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
        public List<TagCounterUI> ConvertTagCounters(List<MetricTagCounter> metricTagCounters)
        {
            List<TagCounterUI> result = new List<TagCounterUI>();
            //TagCountersUI is liquid compatible while TagCounters is not to support json serialization; the split prevents exception
            //not fixable via json iteration disabling
            foreach (MetricTagCounter counter in metricTagCounters)
            {
                result.Add(new TagCounterUI
                {
                    Tag = counter.Tag,
                    Count = counter.Count
                });
            }

            return result;
        }


        #endregion

    }


    /// <summary>
    /// Compatible for liquid; used to avoid use in MetaData as it adds unwanted properties to json serialization 
    /// </summary>
    public class TagCounterUI : Drop
    {
        [JsonProperty(PropertyName = "tag")]
        public string Tag { get; set; }
        [JsonProperty(PropertyName = "displayName")]
        public string ShortTag { get; set; }
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }
        [JsonProperty(PropertyName = "includeAsMatch")]
        public bool IncludeAsMatch => false;
    }
}
