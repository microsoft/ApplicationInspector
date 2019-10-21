// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.DevSkim;
using System.IO;
using System.Linq;
using System.Dynamic;
using System.Runtime.Serialization.Json;

namespace Microsoft.AppInspector.CLI.Writers
{
    /// <summary>
    /// Parent wrapper class for representing source characterization parts
    /// Contains data elements that represent post processing options 
    /// Contains data elements that are related to organization and presentation of tagGroups
    /// </summary>
    [Serializable]
    public class AppProfile
    {
        [JsonProperty(PropertyName = "appInspectorVer")]
        public string Version { get; set; }
        [JsonProperty(PropertyName = "sourcePath")]
        public string SourcePath { get; set; }
        [JsonProperty(PropertyName = "appInspectorArgs")]
        public string Args { get; set; }
        [JsonProperty(PropertyName = "dateScanned")]
        public string DateScanned { get; set; }
        [JsonProperty(PropertyName = "rulePaths")]
        public HashSet<string> RulePaths { get { return MetaData.RulePaths; } }
        public AppMetaData MetaData { get; set; }
        //has to be public to be visible to htmlwriter
        [JsonProperty(PropertyName = "TagReportGroupLists")]
        public Dictionary<string, List<TagInfo>> KeyedTagInfoLists { get; }//dynamic lists for grouping tag properties in reporting
        [JsonIgnore]
        public Dictionary<string, List<TagInfo>> KeyedSortedTagInfoLists { get; } //split to avoid json serialization with others
        [JsonIgnore]
        public List<MatchRecord> MatchList { get; set; }//results list of rulesprocessing
        [JsonIgnore]
        public List<TagCategory> TagGroupPreferences { get; set; }//read preferred list of groups and tags for profile page

        
        //Report properties
        [JsonIgnore]
        public bool ExcludeRollup { get; set; }
        [JsonIgnore]
        public bool SimpleTagsOnly { get; set; }
        [JsonIgnore]
        public bool UniqueTagsOnly { get; }
       

        /// <summary>
        /// Constructor initializes several internal lists not populated by rules processing
        /// </summary>
        /// <param name="sourcePath">code</param>
        /// <param name="rulePaths">rules</param>
        /// <param name="excludeRollup">omit aggregated rollup e.g. simple output with matches</param>
        /// <param name="simpleTagsOnly">simple output override</param>
        /// <param name="uniqueTagsOnly">avoid duplicate tag reporting</param>
        public AppProfile(string sourcePath, List<string> rulePaths, bool excludeRollup, bool simpleTagsOnly, bool uniqueTagsOnly)
        {
            SourcePath = sourcePath;
            Version = Program.GetVersion();
            MatchList = new List<MatchRecord>();
            KeyedTagInfoLists = new Dictionary<string, List<TagInfo>>();
            KeyedSortedTagInfoLists = new Dictionary<string, List<TagInfo>>();
            
            //read default/user preferences on what tags to report presence on and groupings
            TagGroupPreferences = JsonConvert.DeserializeObject<List<TagCategory>>(File.ReadAllText(Helper.GetPath(Helper.AppPath.tagGroupPref)));

            ExcludeRollup = excludeRollup;
            SimpleTagsOnly = simpleTagsOnly;
            UniqueTagsOnly = uniqueTagsOnly;

            MetaData = new AppMetaData(sourcePath, rulePaths)
            {
                RulePaths = rulePaths.ToHashSet<string>()
            };

        }

        /// <summary>
        /// Aggregate tags found into lists by organizing into customer preferred
        /// groups of taginfo objects
        /// TagGroupPreferences are organized by category i.e. profile or composition pages then by groups within
        /// file to be read
        /// </summary>
        public void PrepareReport()
        {
            //start with all unique tags to initialize which is then used to sort into groups of tagInfo lists
            MetaData.UniqueTags = GetUniqueTags();

            foreach (TagCategory CategoryTagGroup in TagGroupPreferences)
            {
                foreach (TagGroup tagGroup in CategoryTagGroup.Groups)
                {
                    foreach (TagSearchPattern pattern in tagGroup.Patterns)
                        pattern.Detected = MetaData.UniqueTags.Any(v => v.Contains(pattern.SearchPattern));
                }
            }

            //create simple ranked page lists for sorted display for app defined report page
            KeyedSortedTagInfoLists["tagGrpAllTagsByConfidence"] = GetTagInfoListByConfidence();
            KeyedSortedTagInfoLists["tagGrpAllTagsBySeverity"] =  GetTagInfoListBySeverity();
            KeyedSortedTagInfoLists["tagGrpAllTagsByName"] =  GetTagInfoListByName();

            //create dynamic "category" groups of tags with pattern relationship established from TagReportGroups.json
            //that can be used to populate reports with various attributes for each tag detected
            foreach (TagCategory tagCategory in TagGroupPreferences)
            {
                foreach (TagGroup group in tagCategory.Groups)
                {
                    if (tagCategory.Type == TagCategory.tagInfoType.uniqueTags)
                        KeyedTagInfoLists["tagGrp" + group.DataRef] = GetUniqueMatchingTagInfoList(group);
                    else if (tagCategory.Type == TagCategory.tagInfoType.allTags)
                        KeyedTagInfoLists["tagGrp" + group.DataRef] = GetAllMatchingTagInfoList(group);
                }
            }


            //TBD: Alternative to use here instead of AnalyzeCommand
            //What is missed if called here instead of Analyze which may not add duplicates?
            //allow each record to be tested for presence of customizable preferred tags
            /*foreach (MatchRecord matchRecord in MatchList)
            {
                MetaData.AddStandardProperties(matchRecord);
            }*/

        }



        #region TagListGroupingMethods

        /// <summary>
        /// Retrieve the set of groups of tags for specified file section in TagReportGroups.json read previously i.e. in PrepareReport
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public List<TagInfo> GetTagInfoList(string category)
        {
            List<TagInfo> result = new List<TagInfo>();
            List<TagGroup> tagGroups = GetCategoryTagGroups(category);
            foreach (TagGroup group in tagGroups)
            {
                result.AddRange(KeyedTagInfoLists["tagGrp" + group.DataRef]);
            }

            return result;
        }



        /// <summary>
        /// Get a list of taggroups for a given file name e.g. profile or composition
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public List<TagGroup> GetCategoryTagGroups(string category)
        {
            List<TagGroup> result = new List<TagGroup>();
            foreach (TagCategory categoryTagGroup in TagGroupPreferences)
            {
                if (categoryTagGroup.Name == category)
                {
                    result = categoryTagGroup.Groups;
                    break;
                }
            }

            foreach (TagGroup group in result)
            {
                GetUniqueMatchingTagInfoList(group);
            }

            return result;
        }



        private HashSet<string> GetUniqueTags()
        {
            HashSet<string> results = new HashSet<string>();

            foreach (MatchRecord match in MatchList)
            {
                foreach (string tag in match.Issue.Rule.Tags)
                    results.Add(tag);
            }

            return results;
        }



        /// <summary>
        /// Builds list of matching tags by profile pattern
        /// Ensures only one instance of a given tag in results unlike GetAllMatchingTags method
        /// </summary>
        /// <param name="tagPattern"></param>
        /// <returns></returns>
        private List<TagInfo> GetUniqueMatchingTagInfoList(TagGroup tagGroup, bool addNotFound=true)
        {
            List<TagInfo> result = new List<TagInfo>();
            HashSet<string> hashSet = new HashSet<string>();

            foreach (TagSearchPattern pattern in tagGroup.Patterns)
            {
                if (pattern.Detected)//set at program.RollUp already so don't search for again
                {
                    var tagPatternRegex = new Regex(pattern.SearchPattern, RegexOptions.IgnoreCase);

                    foreach (var match in MatchList)
                    {
                        foreach (var tagItem in match.Issue.Rule.Tags)
                        {
                            if (tagPatternRegex.IsMatch(tagItem))
                            {
                                if (!hashSet.Contains(pattern.SearchPattern))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = tagItem,
                                        Confidence = match.Issue.Confidence.ToString(),
                                        Severity = match.Issue.Rule.Severity.ToString(),
                                        ShortTag = pattern.DisplayName,
                                        StatusIcon = pattern.DetectedIcon,
                                        Detected = true
                                    }) ;

                                    hashSet.Add(pattern.SearchPattern);

                                    pattern.Confidence = match.Issue.Confidence.ToString();

                                }
                                else //already have in results but...
                                {//ensure we have highest confidence, severity as there are likly multiple matches for this tag pattern
                                    foreach (TagInfo updateItem in result)
                                    {
                                        if (updateItem.Tag == tagItem)
                                        {
                                            DevSkim.Confidence oldConfidence;
                                            Enum.TryParse(updateItem.Confidence, out oldConfidence);
                                            if (match.Issue.Confidence > oldConfidence)
                                            {
                                                updateItem.Confidence = match.Issue.Confidence.ToString();
                                                pattern.Confidence = match.Issue.Confidence.ToString();
                                            }

                                            DevSkim.Severity oldSeverity;
                                            Enum.TryParse(updateItem.Severity, out oldSeverity);
                                            if (match.Issue.Rule.Severity > oldSeverity)
                                            {
                                                updateItem.Severity = match.Issue.Rule.Severity.ToString();
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (addNotFound) //allow page to report on false presense items
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
        /// Gets a set of matching tags for a set of patterns returning for all matches
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

                    foreach (var match in MatchList)
                    {
                        foreach (var tagItem in match.Issue.Rule.Tags)
                        {
                            if (tagPatternRegex.IsMatch(tagItem))
                            {
                                if (!hashSet.Contains(tagItem))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = tagItem,
                                        Confidence = match.Issue.PatternMatch.Confidence.ToString(),
                                        Severity = match.Issue.Rule.Severity.ToString(),
                                        ShortTag = tagItem.Substring(tagItem.LastIndexOf('.') + 1),
                                        StatusIcon = pattern.DetectedIcon,
                                        Detected = true                                 
                                    });

                                    hashSet.Add(tagItem);
                                }
                                else
                                {//ensure we have highest confidence, severity as there are likly multiple matches for this tag pattern
                                    foreach (TagInfo updateItem in result)
                                    {
                                        if (updateItem.Tag == tagItem)
                                        {
                                            DevSkim.Confidence oldConfidence;
                                            Enum.TryParse(updateItem.Confidence, out oldConfidence);
                                            if (match.Issue.PatternMatch.Confidence > oldConfidence)
                                            {
                                                updateItem.Confidence = match.Issue.PatternMatch.Confidence.ToString();
                                            }

                                            DevSkim.Severity oldSeverity;
                                            Enum.TryParse(updateItem.Severity, out oldSeverity);
                                            if (match.Issue.Rule.Severity > oldSeverity)
                                            {
                                                updateItem.Severity = match.Issue.Rule.Severity.ToString();
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


        #endregion  


        #region SortTagsMethods

        /// <summary>
        /// List of taginfo items ordered by name
        /// </summary>
        /// <returns></returns>
        private List<TagInfo> GetTagInfoListByName()
        {
            List<string> orderedTags = MetaData.UniqueTags.ToList<string>();
            orderedTags.Sort();
            HashSet<string> dupCheck = new HashSet<string>();
            List<TagInfo> result = new List<TagInfo>();

            foreach (string tag in orderedTags)
            {
                var searchPattern = new Regex(tag, RegexOptions.IgnoreCase);
                foreach (var match in MatchList)
                {
                    foreach (string testTag in match.Issue.Rule.Tags)
                    {
                        if (searchPattern.IsMatch(tag) && dupCheck.Add(testTag))
                        {
                            result.Add(new TagInfo
                            {
                                Tag = testTag,
                                Confidence = match.Issue.PatternMatch.Confidence.ToString(),
                                Severity = match.Issue.Rule.Severity.ToString(),
                                ShortTag = testTag.Substring(testTag.LastIndexOf('.') + 1),
                            });

                            break;
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
            DevSkim.Confidence[] confidences = { Confidence.High, Confidence.Medium, Confidence.Low };

            foreach (Confidence test in confidences)
            {
                foreach (string tag in MetaData.UniqueTags)
                {
                    var searchPattern = new Regex(tag, RegexOptions.IgnoreCase);
                    foreach (var match in MatchList)
                    {
                        foreach (string testTag in match.Issue.Rule.Tags)
                        {
                            if (searchPattern.IsMatch(testTag))
                            {
                                if (match.Issue.PatternMatch.Confidence == test && dupCheck.Add(tag))
                                    result.Add(new TagInfo
                                    {
                                        Tag = testTag,
                                        Confidence = test.ToString(),
                                        Severity = match.Issue.Rule.Severity.ToString(),
                                        ShortTag = testTag.Substring(testTag.LastIndexOf('.') + 1),
                                    });
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
            DevSkim.Severity[] severities = { Severity.Critical, Severity.Important, Severity.Moderate, Severity.BestPractice, Severity.ManualReview };

            foreach (Severity test in severities)
            {
                foreach (string tag in MetaData.UniqueTags)
                {
                    var searchPattern = new Regex(tag, RegexOptions.IgnoreCase);
                    foreach (var match in MatchList)
                    {
                        foreach (string testTag in match.Issue.Rule.Tags)
                        {
                            if (searchPattern.IsMatch(testTag))
                            {
                                if (match.Issue.Rule.Severity == test && dupCheck.Add(tag))
                                    result.Add(new TagInfo
                                    {
                                        Tag = testTag,
                                        Confidence = match.Issue.PatternMatch.Confidence.ToString(),
                                        Severity = test.ToString(),
                                        ShortTag = testTag.Substring(testTag.LastIndexOf('.') + 1),
                                    });
                            }
                        }
                    }
                }
            }


            return result;
        }

        #endregion

      
    }


    /// <summary>
    /// Contains meta data elements around the source scanned
    /// Contains rollup data for reporting purposes
    /// </summary>
    [Serializable]
    public class AppMetaData
    {
        //Multi-list of elements makes it easier to pass to HTML template engine -direct getters also work
        [JsonIgnore]
        private Dictionary<string, string> _propertyTagSearchPatterns;

        [JsonIgnore] //named properties below will handle for serialization
        public Dictionary<string, HashSet<string>> KeyedPropertyLists { get; }


        public AppMetaData(string sourcePath, List<string> rulePaths)
        {
            //Initial value for ApplicationName may be replaced if rule pattern match found later
            if (Directory.Exists(sourcePath))
            {
                try
                {
                    ApplicationName = sourcePath.Substring(sourcePath.LastIndexOf(Path.DirectorySeparatorChar)).Replace(Path.DirectorySeparatorChar, ' ').Trim();
                }
                catch (Exception)
                {
                    ApplicationName = Path.GetFileNameWithoutExtension(sourcePath);
                }
            }
            else
            {
                ApplicationName = Path.GetFileNameWithoutExtension(sourcePath);
            }
            //initialize set groups of dynamic lists variables that may have more than one value; some are filled
            //using tag tests and others by different means like file type examination
            KeyedPropertyLists = new Dictionary<string, HashSet<string>>
            {
                ["strGrpRulePaths"] = rulePaths.ToHashSet(),
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
            
            //predefined standard tags to track; only some are propertygrouplist are tag based
            _propertyTagSearchPatterns = new Dictionary<string, string>();
            _propertyTagSearchPatterns.Add("strGrpOSTargets", ".OS.Targets");
            _propertyTagSearchPatterns.Add("strGrpCloudTargets", ".Cloud");
            _propertyTagSearchPatterns.Add("strGrpOutputs", ".Outputs");
            _propertyTagSearchPatterns.Add("strGrpCPUTargets", ".CPU");

            //read default/user preferences on what tags to count
            TagCounters = JsonConvert.DeserializeObject<List<TagCounter>>(File.ReadAllText(Helper.GetPath(Helper.AppPath.tagCounterPref)));
            HashSet<string> dupCountersCheck = new HashSet<string>();
            foreach (TagCounter counter in TagCounters)
                if (!dupCountersCheck.Add(counter.Tag))
                    throw new Exception("Duplicate tagCounter found in TagCounters.json preferences file");

            Languages = new Dictionary<string, int>();

        }

        public void AddLanguage(string language)
        {
            if (Languages.ContainsKey(language))
                Languages[language]++;
            else
                Languages.Add(language, 1);
        }


        //simple properties 
        [JsonProperty(PropertyName = "applicationName")]
        public string ApplicationName { get; set; }
        [JsonProperty(PropertyName = "sourceVersion")]
        public string SourceVersion { get; set; }
        [JsonProperty(PropertyName = "authors")]
        public string Authors { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        
        private DateTime _lastUpdated = DateTime.MinValue;
        [JsonProperty(PropertyName = "lastUpdated")]
        public string LastUpdated { get; set; }
        
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
        public int UniqueMatchesCount { get { return UniqueTags.Count; } }
        [JsonIgnore]
        public Dictionary<string, TagCounter> KeyedPropertyCounters { get; }
        [JsonProperty("tagCounters")]
        public List<TagCounter> TagCounters { get; }
        //predefined lists in KeyedPropertyLists for easy retrieval and loose coupling
        [JsonProperty(PropertyName = "packageTypes")]
        public HashSet<string> PackageTypes { get { return KeyedPropertyLists["strGrpPackageTypes"]; } }
        [JsonProperty(PropertyName = "appTypes")]
        public HashSet<string> AppTypes { get { return KeyedPropertyLists["strGrpAppTypes"]; } }
        [JsonIgnore]
        public HashSet<string> RulePaths { get { return KeyedPropertyLists["strGrpRulePaths"]; } set { KeyedPropertyLists["strGrpRulePaths"] = value; } }
        [JsonIgnore]
        public HashSet<string> FileNames { get { return KeyedPropertyLists["strGrpFileNames"]; } }
        [JsonProperty(PropertyName = "uniqueTags")]
        public HashSet<string> UniqueTags
        {
            get
            {
                return KeyedPropertyLists["strGrpUniqueTags"];
            }
            set
            {
                KeyedPropertyLists["strGrpUniqueTags"] = value;
            }
        }

        //convenience getters for standard lists of values
        [JsonProperty(PropertyName = "uniqueDependencies")]
        public HashSet<string> UniqueDependencies { get { return KeyedPropertyLists["strGrpUniqueDependencies"]; } }
        [JsonProperty(PropertyName = "outputs")]
        public HashSet<string> Outputs { get { return KeyedPropertyLists["strGrpOutputs"]; } }
        [JsonProperty(PropertyName = "targets")]
        public HashSet<string> Targets { get { return KeyedPropertyLists["strGrpTargets"]; } }
        [JsonProperty(PropertyName = "languages")]
        public Dictionary<string, int> Languages;     
        [JsonProperty(PropertyName = "OSTargets")]
        public HashSet<string> OSTargets { get { return KeyedPropertyLists["strGrpOSTargets"]; } }
        [JsonProperty(PropertyName = "fileExtensions")]
        public HashSet<string> FileExtensions
        { get { return KeyedPropertyLists["strGrpFileExtensions"]; } }
        [JsonProperty(PropertyName = "cloudTargets")]
        public HashSet<string> CloudTargets { get { return KeyedPropertyLists["strGrpCloudTargets"]; } }
        [JsonProperty(PropertyName = "CPUTargets")]
        public HashSet<string> CPUTargets { get { return KeyedPropertyLists["strGrpCPUTargets"]; } }

        private string ExtractJSONValue(string s)
        {
            try
            {
                var parts = s.Split(':');
                var value = parts[1];
                value = value.Replace("\"", "");
                value = value.Trim();
                return value;
            } catch(Exception)
            {
                return s;
            }

            
        }

        /// <summary>
        /// Part of post processing to test for matches against app defined properties
        /// defined in MetaData class
        /// TODO: decide if we can just call from AppProfile PrepareReport instead
        /// </summary>
        /// <param name="matchRecord"></param>
        public bool AddStandardProperties(MatchRecord matchRecord)
        {
            bool includeAsMatch = true;

            //standard testing for presence of a tag against the preffered set of tags
            foreach (string key in _propertyTagSearchPatterns.Keys)
            {
                var tagPatternRegex = new Regex(_propertyTagSearchPatterns[key], RegexOptions.IgnoreCase);
                if (matchRecord.Issue.Rule.Tags.Any(v => tagPatternRegex.IsMatch(v)))
                {
                    KeyedPropertyLists[key].Add(matchRecord.TextSample);
                }
            }

            //update counts for default or user specified tags

            foreach (TagCounter counter in TagCounters)
            {
                if (matchRecord.Issue.Rule.Tags.Any(v => v.Contains(counter.Tag)))
                {
                    counter.Count++;
                    includeAsMatch = counter.IncludeAsMatch;
                }
            }

            // Author
            if (matchRecord.Issue.Rule.Tags.Contains("Metadata.Application.Author"))
                this.Authors = ExtractJSONValue(matchRecord.TextSample);
            if (matchRecord.Issue.Rule.Tags.Contains("Metadata.Application.Description"))
                this.Description = ExtractJSONValue(matchRecord.TextSample);
            if (matchRecord.Issue.Rule.Tags.Contains("Metadata.Application.Name"))
                this.ApplicationName = ExtractJSONValue(matchRecord.TextSample);
            if (matchRecord.Issue.Rule.Tags.Contains("Metadata.Application.Version"))
                this.SourceVersion = ExtractJSONValue(matchRecord.TextSample);

            //special handling; attempt to detect app types...review for multiple tag limiation
            String solutionType = Helper.DetectSolutionType(matchRecord.Filename, matchRecord.Language, matchRecord.Issue.Rule.Tags[0], matchRecord.TextSample);
            if (!string.IsNullOrEmpty(solutionType) && !AppTypes.Contains(solutionType))
                AppTypes.Add(solutionType);

            #region wip
            //special handling solution name; for efficency (?) use separate tests; note Tags is not an ienumerable
            //TODO this needs work as the text sample of PY projects is not compatible; check Rule accuracy
            /*
            foreach (string tag in matchRecord.Issue.Rule.Tags)
            {
                if (tag.Contains("Solution.Name"))
                {
                    int index = matchRecord.TextSample.IndexOf("\"");
                    if (-1 != index)
                    {
                        ApplicationName = matchRecord.TextSample.Substring(index + 1);
                        ApplicationName = ApplicationName.Replace("\"", "");
                        ApplicationName = ApplicationName.Trim();
                    }
                    else
                        ApplicationName = matchRecord.TextSample;

                    break;
                }
            }
            

            //special handling for version possible format of textMatch i.e. <version="1.0"...       
            foreach (string tag in matchRecord.Issue.Rule.Tags)
            {
                if (tag.Contains("Solution.Version"))
                {
                    int index = matchRecord.TextSample.IndexOf("\"");
                    if (-1 != index)
                    {
                        SourceVersion = matchRecord.TextSample.Substring(index + 1);
                        index = SourceVersion.IndexOf("\"");
                        if (-1 != index)
                            SourceVersion = SourceVersion.Substring(0, index);

                        SourceVersion = SourceVersion.Trim();
                    }
                    else
                        SourceVersion = System.Net.WebUtility.HtmlEncode(matchRecord.TextSample);
                    break;
                }
            }*/
            #endregion

            return includeAsMatch;

        }

    }

}