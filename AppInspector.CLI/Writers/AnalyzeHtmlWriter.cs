// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using DotLiquid;
using DotLiquid.FileSystems;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Microsoft.ApplicationInspector.CLI;

public class AnalyzeHtmlWriter : CommandResultsWriter
{
    private readonly ILogger<AnalyzeHtmlWriter> _logger;
    private AnalyzeResult? _analyzeResult;

    private MetaData? _appMetaData;

    public AnalyzeHtmlWriter(StreamWriter textWriter, ILoggerFactory? loggerFactory = null) : base(textWriter)
    {
        _logger = loggerFactory?.CreateLogger<AnalyzeHtmlWriter>() ?? NullLogger<AnalyzeHtmlWriter>.Instance;
        KeyedTagInfoLists = new Dictionary<string, List<TagInfo>>();
        KeyedSortedTagInfoLists = new Dictionary<string, List<TagInfo>>();
    }

    public Dictionary<string, List<TagInfo>> KeyedTagInfoLists { get; } = new();

    public Dictionary<string, List<TagInfo>> KeyedSortedTagInfoLists { get; } = new();

    public List<TagCategory>?
        TagGroupPreferences { get; set; } //read preferred list of groups and tags for profile / features page

    /// <summary>
    ///     Pre: AnalyzeCommand GetResults created and populated from RulesEngine
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
        WriteHtmlResult();
    }

    private void WriteHtmlResult()
    {
        RenderResultsSafeforHTML();

        //Grab any local css and js files that are needed i.e. don't have hosted URL's or are proprietary
        var allCSS = "<style>\n" +
                     MergeResourceFiles(Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "html", "resources",
                         "css"));
        var allJS = "<script type=\"text/javascript\">\n" +
                    MergeResourceFiles(Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "html", "resources", "js"));

        //Prepare html template merge
        var htmlTemplateText = File.ReadAllText(Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "html/index.html"));
        Template.FileSystem = new EmbeddedFileSystem(Assembly.GetEntryAssembly(),
            "Microsoft.ApplicationInspector.CLI.html.partials");

        //Update template with local aggregated code for easy relocation of output file
        htmlTemplateText = htmlTemplateText.Replace("<script type=\"text/javascript\">", allJS);
        htmlTemplateText =
            htmlTemplateText.Replace(
                "<link rel=\"stylesheet\" type=\"text/css\" href=\"html/resources/css/appinspector.css\" />",
                allCSS + "</style>");

        RegisterSafeType(typeof(MetaData));

        //Prepare data for use in appinspector.js and html partials resources
        var htmlTemplate = Template.Parse(htmlTemplateText);
        var data = new Dictionary<string, object>();
        data["MetaData"] = _appMetaData ?? new MetaData("", "");

        var hashData = new Hash();
        string? jsonData;
        try
        {
            jsonData = JsonSerializer.Serialize(data);
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Failed to write HTML report. Failed to serialize JSON representation of results in memory. {Type} : {Message}",
                e.GetType().Name, e.Message);
            throw;
        }

        hashData["json"] = jsonData; //json serialization required for [js] access to objects
        hashData["application_version"] = Utils.GetVersionString();

        //add dynamic sets of groups of taginfo read from preferences for Profile page
        var tagGroupList = GetCategoryTagGroups("profile");
        hashData["groups"] = tagGroupList;

        //add summary values for sorted tags lists of taginfo
        foreach (var outerKey in KeyedSortedTagInfoLists.Keys)
            hashData.Add(outerKey, KeyedSortedTagInfoLists[outerKey]);

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

    private void RegisterSafeType(Type type)
    {
        Template.RegisterSafeType(type, t => t.ToString());
        Template.RegisterSafeType(type, type.GetMembers(BindingFlags.Instance).Select(e => e.Name).ToArray());
    }

    private string MergeResourceFiles(string inputPath)
    {
        StringBuilder stringBuilder = new();

        if (Directory.Exists(inputPath))
        {
            try
            {
                var srcfileList = Directory.EnumerateFiles(inputPath, "*.*", SearchOption.AllDirectories);
                if (!srcfileList.Any())
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, inputPath));
                }

                foreach (var fileName in srcfileList)
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


    /// <summary>
    ///     Renders code i.e. user input safe for html display for default report
    ///     Delayed html encoding allows original values to be rendered for the output form i.e.
    ///     json, text formats, even nuget objects retain the originals for readability and
    ///     for managing output transformations as desired
    /// </summary>
    private void RenderResultsSafeforHTML()
    {
        //safeguard simple string meta-data
        if (_appMetaData?.ApplicationName != null)
        {
            _appMetaData.ApplicationName = SafeString(_appMetaData?.ApplicationName);
        }

        if (_appMetaData?.Description != null)
        {
            _appMetaData.Description = SafeString(_appMetaData?.Description);
        }

        if (_appMetaData?.Authors != null)
        {
            _appMetaData.Authors = SafeString(_appMetaData?.Authors);
        }

        if (_appMetaData?.SourceVersion != null)
        {
            _appMetaData.SourceVersion = SafeString(_appMetaData?.SourceVersion);
        }

        //safeguard lists data
        SafeList(_appMetaData?.AppTypes);
        SafeList(_appMetaData?.CloudTargets);
        SafeList(_appMetaData?.OSTargets);
        SafeList(_appMetaData?.Outputs);
        SafeList(_appMetaData?.PackageTypes);
        SafeList(_appMetaData?.Targets);
        SafeList(_appMetaData?.CPUTargets);

        //safeguard displayable fields in match records
        foreach (var matchRecord in _appMetaData?.Matches ?? new List<MatchRecord>())
        {
            //safeguard sample output now that we've matched properties for blocking browser xss
            matchRecord.Sample = WebUtility.HtmlEncode(matchRecord.Sample);
            matchRecord.Excerpt = WebUtility.HtmlEncode(matchRecord.Excerpt);
        }
    }

    private void SafeList(List<string>? valuesList)
    {
        for (var i = 0; i < valuesList?.Count; i++) valuesList[i] = SafeString(valuesList[i]);
    }

    private string SafeString(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            return WebUtility.HtmlEncode(value);
        }

        return "";
    }

    /// <summary>
    ///     Processing for organizing results into easy to use TagGroups for customizable display organization in HTML UI
    /// </summary>
    public void PopulateTagGroups()
    {
        //read default/user preferences on what tags to report presence on and groupings
        if (File.Exists(Utils.GetPath(Utils.AppPath.tagGroupPref)))
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip, // Allow strings to start with '/', e.g.,	"// Copyright (C) Microsoft. All rights reserved"
                };
                TagGroupPreferences =
                    JsonSerializer.Deserialize<List<TagCategory>>(
                        File.ReadAllText(Utils.GetPath(Utils.AppPath.tagGroupPref)), options);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    "Failed to populate tag groups. Failed to serialize JSON representation of results in memory. {Type} : {Message}",
                    e.GetType().Name, e.Message);
                throw;
            }
        }
        else
        {
            TagGroupPreferences = new List<TagCategory>();
        }

        string[] unSupportedGroupsOrPatterns = { "metric", "dependency" };

        //for each preferred group of tag patterns determine if at least one instance was detected
        foreach (var tagCategory in TagGroupPreferences ?? new List<TagCategory>())
        foreach (var tagGroup in tagCategory.Groups ?? new List<TagGroup>())
        {
            if (string.IsNullOrEmpty(tagGroup.Title))
            {
                _logger.LogWarning("Tag group with no title skipped");
                continue;
            }

            var test = tagGroup.Title.ToLower().Contains(unSupportedGroupsOrPatterns[0]);
            if (unSupportedGroupsOrPatterns.Any(x => tagGroup.Title.ToLower().Contains(x)))
            {
                _logger.LogWarning(
                    "Unsupported tag group or pattern detected '{title}'.  See online documentation at https://github.com/microsoft/ApplicationInspector/wiki/3.5-Tags",
                    tagGroup.Title);
            }

            foreach (var pattern in tagGroup.Patterns ?? new List<TagSearchPattern>())
            {
                pattern.Detected = _appMetaData?.UniqueTags is not null &&
                                   _appMetaData.UniqueTags.Any(v => v == pattern.SearchPattern);
                if (unSupportedGroupsOrPatterns.Any(x => pattern.SearchPattern.ToLower().Contains(x)))
                {
                    _logger.LogWarning(
                        "Unsupported tag group or pattern detected '{pattern}'.  See online documentation at https://github.com/microsoft/ApplicationInspector/wiki/3.5-Tags",
                        pattern.SearchPattern);
                }

                //create dynamic "category" groups of tags with pattern relationship established from TagReportGroups.json
                //that can be used to populate reports with various attributes for each tag detected
                if (pattern.Detected)
                {
                    var uniqueTagsOnly = tagCategory.Type == TagCategory.tagInfoType.uniqueTags;
                    KeyedTagInfoLists["tagGrp" + tagGroup.DataRef] = GetTagInfoListByTagGroup(tagGroup, uniqueTagsOnly);
                }
            }
        }

        //create simple ranked page lists HTML use
        KeyedSortedTagInfoLists["tagGrpAllTagsByConfidence"] = GetTagInfoListByConfidence();
        KeyedSortedTagInfoLists["tagGrpAllTagsBySeverity"] = GetTagInfoListBySeverity();
        KeyedSortedTagInfoLists["tagGrpAllTagsByName"] = GetTagInfoListByName();
    }

    /// <summary>
    ///     Get a list of TagGroup for a given category section name e.g. profile
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public List<TagGroup> GetCategoryTagGroups(string category)
    {
        List<TagGroup> result = new();
        //get all tag groups for specified category
        foreach (var categoryTagGroup in TagGroupPreferences ?? new List<TagCategory>())
            if (categoryTagGroup.Name == category)
            {
                result = categoryTagGroup.Groups ?? new List<TagGroup>();
                break;
            }

        //now get all matches for that group i.e. Authentication
        foreach (var group in result) GetTagInfoListByTagGroup(group);

        return result;
    }

    /// <summary>
    ///     MetaData.UniqueTags should already exists and be created incrementally but here in case
    /// </summary>
    /// <returns></returns>
    private HashSet<string> GetUniqueTags()
    {
        HashSet<string> results = new();

        foreach (var match in _appMetaData?.Matches ?? new List<MatchRecord>())
        foreach (var tag in match.Tags ?? Array.Empty<string>())
            results.Add(tag);

        return results;
    }

    /// <summary>
    ///     Builds list of matching tags by profile pattern
    ///     Ensures only one instance of a given tag in results unlike GetAllMatchingTags method
    ///     with highest confidence level for that tag pattern
    /// </summary>
    /// <param name="tagPattern"></param>
    /// <returns></returns>
    private List<TagInfo> GetTagInfoListByTagGroup(TagGroup tagGroup, bool addNotFound = true)
    {
        List<TagInfo> result = new();
        HashSet<string> hashSet = new();

        foreach (var pattern in tagGroup.Patterns ?? new List<TagSearchPattern>())
            if (pattern.Detected) //set at program.RollUp already so don't search for again
            {
                var tagPatternRegex = pattern.Expression;
                if (_appMetaData?.TotalMatchesCount > 0)
                {
                    foreach (var match in _appMetaData?.Matches ?? new List<MatchRecord>())
                    foreach (var tagItem in match.Tags ?? Array.Empty<string>())
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
                                foreach (var updateItem in result)
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
                else
                {
                    foreach (var tagItem in _appMetaData?.UniqueTags ?? new List<string>())
                        if (tagPatternRegex.IsMatch(tagItem) && !hashSet.Contains(pattern.SearchPattern))
                        {
                            result.Add(new TagInfo
                            {
                                Tag = tagItem,
                                ShortTag = pattern.DisplayName,
                                StatusIcon = pattern.DetectedIcon,
                                Detected = true
                            });

                            hashSet.Add(tagItem);
                        }
                }
            }
            else if (addNotFound) //allow to report on false presense items
            {
                TagInfo tagInfo = new()
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

        return result;
    }

    /// <summary>
    ///     Gets a set of matching tags for a set of patterns, returning for all matches
    /// </summary>
    /// <param name="patterns"></param>
    /// <param name="addNotFound"></param>
    /// <returns></returns>
    private List<TagInfo> GetAllMatchingTagInfoList(TagGroup tagGroup, bool addNotFound = true)
    {
        List<TagInfo> result = new();
        HashSet<string> hashSet = new();

        foreach (var pattern in tagGroup.Patterns ?? new List<TagSearchPattern>())
            if (pattern.Detected)
            {
                var tagPatternRegex = pattern.Expression;

                foreach (var match in _appMetaData?.Matches ?? new List<MatchRecord>())
                foreach (var tagItem in match.Tags ?? Array.Empty<string>())
                    if (tagPatternRegex.IsMatch(tagItem))
                    {
                        if (!hashSet.Contains(tagItem))
                        {
                            result.Add(new TagInfo
                            {
                                Tag = tagItem,
                                Confidence = match.Confidence.ToString(),
                                Severity = match.Severity.ToString(),
                                ShortTag = tagItem[(tagItem.LastIndexOf('.') + 1)..],
                                StatusIcon = pattern.DetectedIcon,
                                Detected = true
                            });

                            hashSet.Add(tagItem);
                        }
                        else
                        {
                            //ensure we have highest confidence, severity as there are likly multiple matches for this tag pattern
                            foreach (var updateItem in result)
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

        return result;
    }

    /// <summary>
    ///     List of taginfo items ordered by name
    /// </summary>
    /// <returns></returns>
    private List<TagInfo> GetTagInfoListByName()
    {
        HashSet<string> dupCheck = new();
        List<TagInfo> result = new();

        foreach (var tag in _appMetaData?.UniqueTags ?? new List<string>())
            if (_appMetaData?.TotalMatchesCount > 0)
            {
                foreach (var match in _appMetaData?.Matches ?? new List<MatchRecord>())
                foreach (var testTag in match.Tags ?? Array.Empty<string>())
                    if (tag == testTag)
                    {
                        if (dupCheck.Add(testTag))
                        {
                            result.Add(new TagInfo
                            {
                                Tag = testTag,
                                Confidence = match.Confidence.ToString(),
                                Severity = match.Severity.ToString(),
                                ShortTag = testTag[(testTag.LastIndexOf('.') + 1)..]
                            });

                            break;
                        }
                    }
            }
            else
            {
                result.Add(new TagInfo
                {
                    Tag = tag,
                    ShortTag = tag[(tag.LastIndexOf('.') + 1)..]
                });
            }

        return result;
    }

    /// <summary>
    ///     Tags sorted by confidence
    /// </summary>
    /// <returns></returns>
    private List<TagInfo> GetTagInfoListByConfidence()
    {
        List<TagInfo> result = new();
        HashSet<string> dupCheck = new();
        Confidence[] confidences = { Confidence.High, Confidence.Medium, Confidence.Low };

        foreach (var tag in _appMetaData?.UniqueTags ?? new List<string>())
        {
            var searchPattern = new Regex(tag, RegexOptions.IgnoreCase);
            foreach (var confidence in confidences)
            foreach (var match in _appMetaData?.Matches ?? new List<MatchRecord>())
            foreach (var testTag in match.Tags ?? Array.Empty<string>())
                if (searchPattern.IsMatch(testTag))
                {
                    if (match.Confidence == confidence && dupCheck.Add(tag))
                    {
                        result.Add(new TagInfo
                        {
                            Tag = testTag,
                            Confidence = confidence.ToString(),
                            Severity = match.Severity.ToString(),
                            ShortTag = testTag[(testTag.LastIndexOf('.') + 1)..]
                        });
                    }
                }
        }

        return result;
    }

    /// <summary>
    ///     Sorted by Severity
    /// </summary>
    /// <returns></returns>
    private List<TagInfo> GetTagInfoListBySeverity()
    {
        List<TagInfo> result = new();
        HashSet<string> dupCheck = new();
        Severity[] severities =
            { Severity.Critical, Severity.Important, Severity.Moderate, Severity.BestPractice, Severity.ManualReview };

        foreach (var tag in _appMetaData?.UniqueTags ?? new List<string>())
        {
            var searchPattern = new Regex(tag, RegexOptions.IgnoreCase);
            foreach (var severity in severities)
            foreach (var match in _appMetaData?.Matches ?? new List<MatchRecord>())
            foreach (var testTag in match.Tags ?? Array.Empty<string>())
                if (searchPattern.IsMatch(testTag))
                {
                    if (match.Severity == severity && dupCheck.Add(tag))
                    {
                        result.Add(new TagInfo
                        {
                            Tag = testTag,
                            Confidence = match.Confidence.ToString(),
                            Severity = severity.ToString(),
                            ShortTag = testTag[(testTag.LastIndexOf('.') + 1)..]
                        });
                    }
                }
        }

        return result;
    }

    /// <summary>
    ///     Opportunity for any final data prep before report gen
    /// </summary>
    public List<TagCounterUI> ConvertTagCounters(IEnumerable<MetricTagCounter> metricTagCounters)
    {
        List<TagCounterUI> result = new();
        //TagCountersUI is liquid compatible while TagCounters is not to support json serialization; the split prevents exception
        //not fixable via json iteration disabling

        result.AddRange(metricTagCounters.Select(counter => new TagCounterUI
        {
            Tag = counter.Tag,
            Count = counter.Count
        }));

        return result;
    }
}

/// <summary>
///     Compatible for liquid; used to avoid use in MetaData as it adds unwanted properties to json serialization
/// </summary>
public class TagCounterUI : Drop
{
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    [JsonPropertyName("displayName")]
    public string? ShortTag { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("includeAsMatch")]
    public bool IncludeAsMatch => false;
}