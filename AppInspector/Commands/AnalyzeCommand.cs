// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GlobExpressions;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json.Serialization;
using ShellProgressBar;
using LibGit2Sharp;

namespace Microsoft.ApplicationInspector.Commands;

/// <summary>
///     Options specific to analyze operation not to be confused with CLIAnalyzeCmdOptions which include CLI only args
/// </summary>
public class AnalyzeOptions
{
    /// <summary>
    ///     Any number of source paths to scan.
    /// </summary>
    public IEnumerable<string> SourcePath { get; set; } = Array.Empty<string>();
    /// <summary>
    ///     A path (file or directory) on disk to your custom rule file(s).
    /// </summary>
    public string? CustomRulesPath { get; set; }
    /// <summary>
    ///     Disable the Default ApplicationInspector RuleSet.
    /// </summary>
    public bool IgnoreDefaultRules { get; set; }
    /// <summary>
    ///     Which confidence values to use.
    /// </summary>
    public IEnumerable<Confidence> ConfidenceFilters { get; set; } = new[] { Confidence.High, Confidence.Medium };

    /// <summary>
    ///     Which severity values to use.
    /// </summary>
    public IEnumerable<Severity> SeverityFilters { get; set; } = new[]
        { Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice | Severity.ManualReview };

    /// <summary>
    ///     File paths which should be excluded from scanning.
    /// </summary>
    public IEnumerable<string> FilePathExclusions { get; set; } = Array.Empty<string>();
    /// <summary>
    ///     If enabled, processing will be performed on one file at a time.
    /// </summary>
    public bool SingleThread { get; set; }

    /// <summary>
    ///     Treat <see cref="LanguageInfo.LangFileType.Build" /> files as if they were
    ///     <see cref="LanguageInfo.LangFileType.Code" /> when determining if tags should apply.
    /// </summary>
    public bool AllowAllTagsInBuildFiles { get; set; }

    /// <summary>
    ///     If enabled, will not show the progress bar interface.
    /// </summary>
    public bool NoShowProgress { get; set; } = true;

    /// <summary>
    ///     If enabled, only tags are collected, with no detailed match or file information.
    /// </summary>
    public bool TagsOnly { get; set; }

    /// <summary>
    ///     Amount of time in ms to allow to process each file.  Not supported in async operations.
    /// </summary>
    public int FileTimeOut { get; set; } = 0;

    /// <summary>
    ///     Overall amount of time in ms to allow for processing.  Not supported in async operations.
    /// </summary>
    public int ProcessingTimeOut { get; set; } = 0;

    /// <summary>
    ///     Number of lines of Context to collect from each file for the Excerpt. Set to -1 to disable gathering context entirely.
    /// </summary>
    public int ContextLines { get; set; } = 3;
    /// <summary>
    ///     Run rules against files for which the appropriate Language cannot be determined.
    /// </summary>
    public bool ScanUnknownTypes { get; set; }
    /// <summary>
    ///     Don't gather metadata about the files scanned.
    /// </summary>
    public bool NoFileMetadata { get; set; }

    /// <summary>
    ///     If non-zero, and <see cref="TagsOnly" /> is not set, will ignore matches if all of the matches tags have already
    ///     been found the set value number of times.
    /// </summary>
    public int MaxNumMatchesPerTag { get; set; } = 0;

    /// <summary>
    ///     A path to a custom comments.json file to modify the set of comment styles understood by Application Inspector.
    /// </summary>
    public string? CustomCommentsPath { get; set; }
    /// <summary>
    ///     A path to a custom languages.json file to modify the set of languages understood by Application Inspector.
    /// </summary>
    public string? CustomLanguagesPath { get; set; }

    /// <summary>
    ///     If set, will not crawl archives to scan the contents of the contained files.
    /// </summary>
    public bool DisableCrawlArchives { get; set; }

    /// <summary>
    ///     If <see cref="DisableCrawlArchives" /> is not set, will restrict the amount of time allowed to extract each
    ///     archive. Not supported in async operations.
    /// </summary>
    public int EnumeratingTimeout { get; set; }

    /// <summary>
    ///     By default, custom rules are verified before running.
    /// </summary>
    public bool DisableCustomRuleVerification { get; set; }

    /// <summary>
    ///     By default, rules must have unique IDs.
    /// </summary>
    public bool DisableRequireUniqueIds { get; set; }

    /// <summary>
    ///     Return a success error code when no matches were found but operation was apparently successful. Useful for CI
    ///     scenarios
    /// </summary>
    public bool SuccessErrorCodeOnNoMatches { get; set; }

    /// <summary>
    ///     If set, when validating rules, require that every rule have a must-match self-test with at least one entry
    /// </summary>
    public bool RequireMustMatch { get; set; }
    /// <summary>
    ///     If set, when validating rules, require that every rule have a must-not-match self-test with at least one entry
    /// </summary>
    public bool RequireMustNotMatch { get; set; }

    /// <summary>
    ///     If set, prefer to build rule Regex with the Non-BackTracking engine unless the modifiers contain `b`
    ///     Will fall back to BackTracking engine if the Rule cannot be built with Non-BackTracking.
    /// </summary>
    public bool EnableNonBacktrackingRegex { get; set; }
}

/// <summary>
///     Result of Analyze command GetResult() operation
/// </summary>
public class AnalyzeResult : Result
{
    public enum ExitCode
    {
        Success = 0,
        NoMatches = 1,
        CriticalError = Utils.ExitCode.CriticalError, //ensure common value for final exit log mention
        Canceled = 3,
        TimedOut = 4
    }

    public AnalyzeResult()
    {
        Metadata = new MetaData("", ""); //needed for serialization for other commands; replaced later
    }

    [JsonPropertyName("resultCode")]
    // [JsonPropertyOrder(2)] .NET 6.0 only
    public ExitCode ResultCode { get; set; }

    /// <summary>
    ///     Analyze command result object containing scan properties
    /// </summary>
    [JsonPropertyName("metaData")]
    // [JsonPropertyOrder(3)]
    public MetaData Metadata { get; set; }
}

/// <summary>
///     Analyze operation for setup and processing of results from Rules Engine
/// </summary>
public class AnalyzeCommand
{
    private const int ProgressBarUpdateDelay = 100;
    private readonly Confidence _confidence = Confidence.Unspecified;

    private readonly List<Glob> _fileExclusionList = new();
    private readonly ILogger _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly AnalyzeOptions _options; //copy of incoming caller options
    private readonly Severity _severity = Severity.Unspecified;
    private readonly List<string> _srcfileList = new();
    private readonly Languages _languages = new();
    private MetaDataHelper _metaDataHelper; //wrapper containing MetaData object to be assigned to result
    private readonly RuleProcessor _rulesProcessor;

    /// <summary>
    ///     Constructor for AnalyzeCommand.
    /// </summary>
    /// <param name="opt">The <see cref="AnalyzeOptions" /> to use for this analysis.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory" /> use for log messages.</param>
    public AnalyzeCommand(AnalyzeOptions opt, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory?.CreateLogger<AnalyzeCommand>() ?? NullLogger<AnalyzeCommand>.Instance;
        _options = opt;

        _fileExclusionList = opt.FilePathExclusions.Any(x => !x.Equals("none"))
            ? opt.FilePathExclusions.Select(x => new Glob(x)).ToList()
            : new List<Glob>();

        //create metadata helper to wrap and help populate metadata from scan
        _metaDataHelper = new MetaDataHelper(string.Join(',', _options.SourcePath));
        DateScanned = DateTime.Now;
        foreach (var confidence in _options.ConfidenceFilters) _confidence |= confidence;
        foreach (var severity in _options.SeverityFilters) _severity |= severity;

        _logger.LogTrace("AnalyzeCommand::ConfigSourcetoScan");

        if (!_options.SourcePath.Any())
        {
            throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_REQUIRED_ARG_MISSING, "SourcePath"));
        }

        foreach (var entry in _options.SourcePath)
            if (Directory.Exists(entry))
            {
                _srcfileList.AddRange(Directory.EnumerateFiles(entry, "*.*", SearchOption.AllDirectories));
            }
            else if (File.Exists(entry))
            {
                _srcfileList.Add(entry);
            }
            else
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, entry));
            }

        if (_srcfileList.Count == 0)
        {
            throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_NO_FILES_IN_SOURCE,
                string.Join(',', _options.SourcePath)));
        }

        _logger.LogTrace("AnalyzeCommand::ConfigRules");

        if (!string.IsNullOrEmpty(_options.CustomCommentsPath) || !string.IsNullOrEmpty(_options.CustomLanguagesPath))
        {
            _languages = Languages.FromConfigurationFiles(_loggerFactory, _options.CustomCommentsPath,
                _options.CustomLanguagesPath);
        }

        RuleSet? rulesSet = null;

        if (!_options.IgnoreDefaultRules)
        {
            rulesSet = RuleSetUtils.GetDefaultRuleSet(_loggerFactory, _options.EnableNonBacktrackingRegex);
        }

        if (!string.IsNullOrEmpty(_options.CustomRulesPath))
        {
            rulesSet ??= new RuleSet(_loggerFactory) { EnableNonBacktrackingRegex = _options.EnableNonBacktrackingRegex };
            
            RulesVerifierOptions rulesVerifierOptions = new()
            {
                LanguageSpecs = _languages,
                LoggerFactory = _loggerFactory,
                DisableRequireUniqueIds = _options.DisableRequireUniqueIds,
                RequireMustMatch = _options.RequireMustMatch,
                RequireMustNotMatch = _options.RequireMustNotMatch,
                EnableNonBacktrackingRegex = _options.EnableNonBacktrackingRegex,
            };
            RulesVerifier verifier = new(rulesVerifierOptions);
            var anyFails = false;
            if (Directory.Exists(_options.CustomRulesPath))
            {
                foreach (var filename in Directory.EnumerateFileSystemEntries(_options.CustomRulesPath, "*.json",
                             SearchOption.AllDirectories))
                    VerifyFile(filename);
            }
            else if (File.Exists(_options.CustomRulesPath))
            {
                VerifyFile(_options.CustomRulesPath);
            }
            else
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_RULE_PATH, _options.CustomRulesPath));
            }

            if (anyFails)
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULE_LOADFILE_FAILED,
                    _options.CustomRulesPath));
            }

            void VerifyFile(string filename)
            {
                if (!_options.DisableCustomRuleVerification)
                {
                    var verification = verifier.Verify(_options.CustomRulesPath);
                    if (!verification.Verified)
                    {
                        anyFails = true;
                    }
                    else
                    {
                        rulesSet.AddFile(filename);
                    }
                }
                else
                {
                    rulesSet.AddFile(filename);
                }
            }
        }

        //error check based on ruleset not path enumeration
        if (rulesSet == null || !rulesSet.Any())
        {
            throw new OpException(MsgHelp.GetString(MsgHelp.ID.CMD_NORULES_SPECIFIED));
        }

        //instantiate a RuleProcessor with the added rules and exception for dependency
        RuleProcessorOptions rpo = new()
        {
            LoggerFactory = _loggerFactory,
            AllowAllTagsInBuildFiles = _options.AllowAllTagsInBuildFiles,
            ConfidenceFilter = _confidence,
            SeverityFilter = _severity,
            Parallel = !_options.SingleThread,
            Languages = _languages
        };

        _rulesProcessor = new RuleProcessor(rulesSet, rpo);
    }

    private DateTime DateScanned { get; }

    /// <summary>
    ///     Populate the MetaDataHelper with the data from the FileEntries
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="populatedEntries"></param>
    /// <returns></returns>
    private AnalyzeResult.ExitCode PopulateRecords(CancellationToken cancellationToken,
        IEnumerable<FileEntry>? populatedEntries)
    {
        _logger.LogTrace("AnalyzeCommand::PopulateRecords");
        populatedEntries ??= EnumerateFileEntries();
        if (_metaDataHelper is null)
        {
            _logger.LogError("MetadataHelper is null");
            throw new NullReferenceException("_metaDataHelper");
        }

        if (_options.SingleThread)
        {
            foreach (var entry in populatedEntries)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return AnalyzeResult.ExitCode.Canceled;
                }

                ProcessAndAddToMetadata(entry);
            }
        }
        else
        {
            try
            {
                Parallel.ForEach(populatedEntries, new ParallelOptions { CancellationToken = cancellationToken },
                    ProcessAndAddToMetadata);
            }
            catch (OperationCanceledException)
            {
                return AnalyzeResult.ExitCode.Canceled;
            }
        }

        if (!_options.TagsOnly)
        {
            RemoveDependsOnNotPresent();
        }

        _metaDataHelper.AddGitInformation(GenerateGitInformation(Path.GetFullPath(_options.SourcePath.FirstOrDefault())));

        return AnalyzeResult.ExitCode.Success;

        void ProcessAndAddToMetadata(FileEntry file)
        {
            FileRecord fileRecord = new()
            {
                FileName = file.FullPath, ModifyTime = file.ModifyTime, CreateTime = file.CreateTime,
                AccessTime = file.AccessTime
            };

            Stopwatch sw = new();
            sw.Start();

            if (_fileExclusionList.Any(x => x.IsMatch(file.FullPath)))
            {
                _logger.LogDebug(MsgHelp.GetString(MsgHelp.ID.ANALYZE_EXCLUDED_TYPE_SKIPPED), fileRecord.FileName);
                fileRecord.Status = ScanState.Skipped;
            }
            else
            {
                if (IsBinary(file.Content))
                {
                    _logger.LogDebug(MsgHelp.GetString(MsgHelp.ID.ANALYZE_EXCLUDED_BINARY), fileRecord.FileName);
                    fileRecord.Status = ScanState.Skipped;
                }
                else
                {
                    List<MatchRecord> results = new();

                    // Reusable parsing logic that is used in an anonymous task or called directly depending on timeout preference.
                    void ProcessLambda()
                    {
                        _ = _metaDataHelper.FileExtensions.TryAdd(
                            Path.GetExtension(file.FullPath).Replace('.', ' ').TrimStart(), 0);

                        LanguageInfo languageInfo = new();

                        if (_languages.FromFileName(file.FullPath, ref languageInfo))
                        {
                            _metaDataHelper.AddLanguage(languageInfo.Name);
                        }
                        else
                        {
                            _metaDataHelper.AddLanguage("Unknown");
                            languageInfo = new LanguageInfo
                                { Extensions = new[] { Path.GetExtension(file.FullPath) }, Name = "Unknown" };
                            if (!_options.ScanUnknownTypes)
                            {
                                fileRecord.Status = ScanState.Skipped;
                            }
                        }

                        if (fileRecord.Status != ScanState.Skipped)
                        {
                            if (_options.TagsOnly)
                            {
                                results = _rulesProcessor.AnalyzeFile(file, languageInfo,
                                    _metaDataHelper.UniqueTags.Keys, -1);
                            }
                            else if (_options.MaxNumMatchesPerTag > 0)
                            {
                                results = _rulesProcessor.AnalyzeFile(file, languageInfo,
                                    _metaDataHelper.UniqueTags.Where(x => x.Value >= _options.MaxNumMatchesPerTag)
                                        .Select(x => x.Key), _options.ContextLines);
                            }
                            else
                            {
                                results = _rulesProcessor.AnalyzeFile(file, languageInfo, null, _options.ContextLines);
                            }
                        }
                    }

                    if (_options.FileTimeOut > 0)
                    {
                        using CancellationTokenSource cts = new();
                        var t = Task.Run(ProcessLambda, cts.Token);
                        try
                        {
                            if (!t.Wait(new TimeSpan(0, 0, 0, 0, _options.FileTimeOut)))
                            {
                                _logger.LogError("{Path} timed out", file.FullPath);
                                fileRecord.Status = ScanState.TimedOut;
                                cts.Cancel();
                            }
                            else
                            {
                                fileRecord.Status = ScanState.Analyzed;
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogDebug(
                                "Failed to analyze file {Path}. {Type}:{Message}. ({StackTrace}), fileRecord.FileName",
                                file.FullPath, e.GetType(), e.Message, e.StackTrace);
                            fileRecord.Status = ScanState.Error;
                        }
                    }
                    else
                    {
                        ProcessLambda();
                        fileRecord.Status = ScanState.Analyzed;
                    }

                    if (results.Any())
                    {
                        fileRecord.Status = ScanState.Affected;
                        fileRecord.NumFindings = results.Count;
                    }

                    foreach (var matchRecord in results)
                    {
                        if (_options.TagsOnly)
                        {
                            _metaDataHelper.AddTagsFromMatchRecord(matchRecord);
                        }
                        else if (_options.MaxNumMatchesPerTag > 0)
                        {
                            if (matchRecord.Tags?.Any(x =>
                                    _metaDataHelper.UniqueTags.TryGetValue(x, out var value) is bool foundValue &&
                                    (!foundValue || (foundValue && value < _options.MaxNumMatchesPerTag))) ??
                                false)
                            {
                                _metaDataHelper.AddMatchRecord(matchRecord);
                            }
                        }
                        else
                        {
                            _metaDataHelper.AddMatchRecord(matchRecord);
                        }
                    }
                }
            }

            sw.Stop();

            fileRecord.ScanTime = sw.Elapsed;

            if (!_options.NoFileMetadata)
            {
                _metaDataHelper.Files.Add(fileRecord);
            }
        }
    }

    private GitInformation? GenerateGitInformation(string optsPath)
    {
        try
        {
            using var repo = new Repository(optsPath);
            var info = new GitInformation()
            {
                Branch = repo.Head.FriendlyName
            };
            if (repo.Network.Remotes.Any())
            {
                info.RepositoryUri = new Uri(repo.Network.Remotes.First().Url);
            }
            if (repo.Head.Commits.Any())
            {
                info.CommitHash = repo.Head.Commits.First().Sha;
            }

            return info;
        }
        catch
        {
            if (Directory.GetParent(optsPath) is { } notNullParent)
            {
                return GenerateGitInformation(notNullParent.FullName);
            }
        }

        return null;
    }

    /// <summary>
    /// Remove matches from the metadata when the DependsOnTags are not satisfied.
    /// </summary>
    private void RemoveDependsOnNotPresent()
    {
        bool anyChanges = false;
        List<MatchRecord> previousMatches = _metaDataHelper.Matches.ToList();
        List<MatchRecord> nextMatches = FilterRecordsByMissingDependsOnTags(previousMatches);
        // Continue iterating as long as records were removed in the last iteration, as their tags may have been depended on by another rule
        while (nextMatches.Count != previousMatches.Count)
        {
            anyChanges = true;
            (nextMatches, previousMatches) = (FilterRecordsByMissingDependsOnTags(nextMatches), nextMatches);
        }
        if (anyChanges)
        {
            _metaDataHelper = _metaDataHelper.CreateFresh();
            foreach (MatchRecord matchRecord in nextMatches)
            {
                _metaDataHelper.AddMatchRecord(matchRecord);
            }
        }
    }

    /// <summary>
    /// Return a new List of MatchRecords with records removed which depend on tags not present in the set of records.
    /// Does not modify the original list.
    /// </summary>
    /// <param name="listToFilter"></param>
    /// <returns></returns>
    private List<MatchRecord> FilterRecordsByMissingDependsOnTags(List<MatchRecord> listToFilter)
    {
        HashSet<string> tags = listToFilter.SelectMany(x => x.Tags).Distinct().ToHashSet();
        return listToFilter.Where(x => x.Rule?.DependsOnTags?.All(tag => tags.Contains(tag)) ?? true).ToList();

    }

    /// <summary>
    ///     Populate the records in the metadata asynchronously.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Result code.</returns>
    private async Task<AnalyzeResult.ExitCode> PopulateRecordsAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("AnalyzeCommand::PopulateRecordsAsync");
        if (_metaDataHelper is null)
        {
            _logger.LogError("MetadataHelper is null");
            throw new NullReferenceException("_metaDataHelper");
        }

        await foreach (var entry in GetFileEntriesAsync())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AnalyzeResult.ExitCode.Canceled;
            }

            await ProcessAndAddToMetadata(entry, cancellationToken);
        }

        if (!_options.TagsOnly)
        {
            RemoveDependsOnNotPresent();
        }

        _metaDataHelper.AddGitInformation(GenerateGitInformation(Path.GetFullPath(_options.SourcePath.FirstOrDefault())));

        return AnalyzeResult.ExitCode.Success;

        async Task ProcessAndAddToMetadata(FileEntry file, CancellationToken cancellationToken)
        {
            FileRecord fileRecord = new()
            {
                FileName = file.FullPath, ModifyTime = file.ModifyTime, CreateTime = file.CreateTime,
                AccessTime = file.AccessTime
            };

            Stopwatch sw = new();
            sw.Start();

            if (_fileExclusionList.Any(x => x.IsMatch(file.FullPath)))
            {
                _logger.LogDebug(MsgHelp.GetString(MsgHelp.ID.ANALYZE_EXCLUDED_TYPE_SKIPPED), fileRecord.FileName);
                fileRecord.Status = ScanState.Skipped;
            }
            else
            {
                if (IsBinary(file.Content))
                {
                    _logger.LogDebug(MsgHelp.GetString(MsgHelp.ID.ANALYZE_EXCLUDED_BINARY), fileRecord.FileName);
                    fileRecord.Status = ScanState.Skipped;
                }
                else
                {
                    _ = _metaDataHelper.FileExtensions.TryAdd(
                        Path.GetExtension(file.FullPath).Replace('.', ' ').TrimStart(), 0);

                    LanguageInfo languageInfo = new();

                    if (_languages.FromFileName(file.FullPath, ref languageInfo))
                    {
                        _metaDataHelper.AddLanguage(languageInfo.Name);
                    }
                    else
                    {
                        _metaDataHelper.AddLanguage("Unknown");
                        languageInfo = new LanguageInfo
                            { Extensions = new[] { Path.GetExtension(file.FullPath) }, Name = "Unknown" };
                        if (!_options.ScanUnknownTypes)
                        {
                            fileRecord.Status = ScanState.Skipped;
                        }
                    }

                    if (fileRecord.Status != ScanState.Skipped)
                    {
                        var contextLines = _options.TagsOnly ? -1 : _options.ContextLines;
                        var ignoredTags = _options.TagsOnly ? _metaDataHelper.UniqueTags.Keys :
                            _options.MaxNumMatchesPerTag > 0 ? _metaDataHelper.UniqueTags
                                .Where(x => x.Value < _options.MaxNumMatchesPerTag).Select(x => x.Key) : null;
                        var results = await _rulesProcessor.AnalyzeFileAsync(file, languageInfo, cancellationToken,
                            ignoredTags, contextLines);
                        fileRecord.Status = ScanState.Analyzed;

                        if (results.Any())
                        {
                            fileRecord.Status = ScanState.Affected;
                            fileRecord.NumFindings = results.Count;
                        }

                        foreach (var matchRecord in results)
                        {
                            if (_options.TagsOnly)
                            {
                                _metaDataHelper.AddTagsFromMatchRecord(matchRecord);
                            }
                            else if (_options.MaxNumMatchesPerTag > 0)
                            {
                                if (matchRecord.Tags?.Any(x =>
                                        _metaDataHelper.UniqueTags.TryGetValue(x, out var value) is bool foundValue &&
                                        (!foundValue || (foundValue && value < _options.MaxNumMatchesPerTag))) ??
                                    true)
                                {
                                    _metaDataHelper.AddMatchRecord(matchRecord);
                                }
                            }
                            else
                            {
                                _metaDataHelper.AddMatchRecord(matchRecord);
                            }
                        }
                    }
                }
            }

            sw.Stop();

            fileRecord.ScanTime = sw.Elapsed;

            _metaDataHelper.Files.Add(fileRecord);
        }
    }

    /// <summary>
    ///     Gets the FileEntries synchronously.
    /// </summary>
    /// <returns>An Enumerable of FileEntries.</returns>
    private IEnumerable<FileEntry> EnumerateFileEntries()
    {
        _logger.LogTrace("AnalyzeCommand::EnumerateFileEntries");

        Extractor extractor = new();
        // For every file, if the file isn't excluded return it, and if it is track the exclusion in the metadata
        foreach (var srcFile in _srcfileList)
            if (_fileExclusionList.Any(x => x.IsMatch(srcFile)))
            {
                _metaDataHelper?.Metadata.Files.Add(new FileRecord { FileName = srcFile, Status = ScanState.Skipped });
            }
            else
            {
                Stream? contents = null;
                try
                {
                    contents = File.OpenRead(srcFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to open source file '{Filename}' for reading. {Type}:{Message}", srcFile,
                        ex.GetType().Name, ex.Message);
                    _metaDataHelper?.Metadata.Files.Add(new FileRecord
                        { FileName = srcFile, Status = ScanState.Error });
                }

                if (contents != null)
                {
                    if (_options.DisableCrawlArchives)
                    {
                        yield return new FileEntry(srcFile, contents);
                    }
                    else
                    {
                        // Use MemoryStreamCutoff = 1 to force using FileStream with DeleteOnClose for backing, and avoid memory exhaustion.
                        ExtractorOptions opts = new()
                        {
                            Parallel = false, DenyFilters = _options.FilePathExclusions, MemoryStreamCutoff = 1
                        };
                        // This works if the contents contain any kind of file.
                        // If the file is an archive this gets all the entries it contains.
                        // If the file is not an archive, the stream is wrapped in a FileEntry container and yielded
                        foreach (var entry in extractor.Extract(srcFile, contents, opts)) yield return entry;
                    }
                }

                // Be sure to close the stream after we are done processing it.
                contents?.Dispose();
            }
    }

    /// <summary>
    ///     Gets the FileEntries asynchronously.
    /// </summary>
    /// <returns>An enumeration of FileEntries</returns>
    private async IAsyncEnumerable<FileEntry> GetFileEntriesAsync()
    {
        _logger.LogTrace("AnalyzeCommand::GetFileEntriesAsync");

        Extractor extractor = new();
        foreach (var srcFile in _srcfileList)
            if (_fileExclusionList.Any(x => x.IsMatch(srcFile)))
            {
                _metaDataHelper?.Metadata.Files.Add(new FileRecord { FileName = srcFile, Status = ScanState.Skipped });
            }
            else
            {
                await foreach (var entry in extractor.ExtractAsync(srcFile,
                                   new ExtractorOptions
                                   {
                                       Parallel = false, DenyFilters = _options.FilePathExclusions,
                                       MemoryStreamCutoff = 1
                                   }))
                    yield return entry;
            }
    }


    // Follows Perl's model, if there are NULs or too many non printable characters, this is probably a binary file
    private static bool IsBinary(Stream fileContents)
    {
        var numRead = 1;
        var span = new Span<byte>(new byte[8192]);
        var controlsEncountered = 0;
        var maxControlsEncountered = (int)(0.3 * fileContents.Length);
        while (numRead > 0)
        {
            numRead = fileContents.Read(span);
            for (var i = 0; i < numRead; i++)
            {
                var ch = (char)span[i];
                if (ch == '\0')
                {
                    fileContents.Position = 0;
                    return true;
                }

                if (char.IsControl(ch) && !char.IsWhiteSpace(ch))
                {
                    if (++controlsEncountered > maxControlsEncountered)
                    {
                        fileContents.Position = 0;
                        return true;
                    }
                }
            }
        }

        fileContents.Position = 0;
        return false;
    }

    /// <summary>
    ///     Perform Analysis and get the result Asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop analysis and return results found so far.</param>
    /// <returns></returns>
    public async Task<AnalyzeResult> GetResultAsync(CancellationToken? cancellationToken = null)
    {
        _logger.LogTrace("AnalyzeCommand::GetResultAsync");
        _logger.LogInformation(MsgHelp.GetString(MsgHelp.ID.CMD_RUNNING), "Analyze");
        cancellationToken ??= new CancellationToken();
        if (_metaDataHelper is null)
        {
            _logger.LogError("MetadataHelper is null");
            throw new NullReferenceException("_metaDataHelper");
        }

        AnalyzeResult analyzeResult = new()
        {
            AppVersion = Utils.GetVersionString()
        };

        _ = await PopulateRecordsAsync(cancellationToken.Value);

        if (!_options.SuccessErrorCodeOnNoMatches)
        {
            if (!_options.NoFileMetadata && _metaDataHelper.Files.All(x => x.Status == ScanState.Skipped))
            {
                _logger.LogError(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOSUPPORTED_FILETYPES));
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
            }
            else if (!_metaDataHelper.HasFindings)
            {
                _logger.LogError(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOPATTERNS));
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
            }
        }

        if (_metaDataHelper is { Metadata: { } })
        {
            _metaDataHelper.Metadata.DateScanned = DateScanned.ToString(CultureInfo.InvariantCulture);
            _metaDataHelper.PrepareReport();
            analyzeResult.Metadata = _metaDataHelper.Metadata; //replace instance with metadatahelper processed one
            analyzeResult.ResultCode = AnalyzeResult.ExitCode.Success;
        }

        if (cancellationToken.Value.IsCancellationRequested)
        {
            analyzeResult.ResultCode = AnalyzeResult.ExitCode.Canceled;
        }

        return analyzeResult;
    }

    /// <summary>
    ///     Main entry point to start analysis from CLI; handles setting up rules, directory enumeration
    ///     file type detection and handoff
    ///     Pre: All Configure Methods have been called already and we are ready to SCAN
    /// </summary>
    /// <returns></returns>
    public AnalyzeResult GetResult()
    {
        _logger.LogTrace("AnalyzeCommand::GetResultAsync");
        _logger.LogInformation(MsgHelp.GetString(MsgHelp.ID.CMD_RUNNING), "Analyze");
        if (_metaDataHelper is null)
        {
            _logger.LogError("MetadataHelper is null");
            throw new NullReferenceException("_metaDataHelper");
        }

        AnalyzeResult analyzeResult = new()
        {
            AppVersion = Utils.GetVersionString()
        };

        var timedOut = false;

        // If progress display is disabled then we can pass the enumerable directly
        if (_options.NoShowProgress)
        {
            IEnumerable<FileEntry> enumeratedEntries = Array.Empty<FileEntry>();
            if (_options.EnumeratingTimeout > 0)
            {
                using CancellationTokenSource cts = new();
                var t = Task.Run(() =>
                {
                    try
                    {
                        enumeratedEntries = EnumerateFileEntries().ToList();
                    }
                    catch (OverflowException e)
                    {
                        _logger.LogError(
                            "Overflowed while extracting file entries. Check the input for quines or zip bombs. {Message}",
                            e.Message);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            "Unexpected error while enumerating files. {Type}:{Message}",
                            e.GetType().Name, e.Message);
                    }
                }, cts.Token);
                if (!t.Wait(new TimeSpan(0, 0, 0, 0, _options.EnumeratingTimeout)))
                {
                    cts.Cancel();
                }
            }
            else
            {
                enumeratedEntries = EnumerateFileEntries();
            }

            DoProcessing(enumeratedEntries);
        }
        else
        {
            var doneEnumerating = false;
            var enumeratingTimedOut = false;
            ConcurrentBag<FileEntry> fileQueue = new();

            using CancellationTokenSource cts = new();
            var t = Task.Run(() =>
            {
                try
                {
                    foreach (var entry in EnumerateFileEntries()) fileQueue.Add(entry);
                }
                catch (OverflowException e)
                {
                    _logger.LogError(
                        "Overflowed while extracting file entries. Check the input for quines or zip bombs. {Message}",
                        e.Message);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        "Unexpected error while enumerating files. {Type}:{Message}",
                        e.GetType().Name, e.Message);
                }
                finally
                {
                    doneEnumerating = true;
                }
            }, cts.Token);
            if (_options.EnumeratingTimeout > 0)
            {
                if (!t.Wait(new TimeSpan(0, 0, 0, 0, _options.EnumeratingTimeout)))
                {
                    enumeratingTimedOut = true;
                    doneEnumerating = true;
                    cts.Cancel();
                }
            }

            ProgressBarOptions options = new()
            {
                ForegroundColor = ConsoleColor.Yellow,
                ForegroundColorDone = ConsoleColor.DarkGreen,
                BackgroundColor = ConsoleColor.DarkGray,
                ForegroundColorError = ConsoleColor.Red,
                BackgroundCharacter = '\u2593',
                DisableBottomPercentage = true
            };

            using (IndeterminateProgressBar pbar = new("Enumerating Files.", options))
            {
                while (!doneEnumerating)
                {
                    Thread.Sleep(ProgressBarUpdateDelay);
                    pbar.Message = $"Enumerating Files. {fileQueue.Count} Discovered so far.";
                }

                pbar.Message = enumeratingTimedOut
                    ? $"Enumerating Files Timed Out. {fileQueue.Count} Discovered."
                    : $"Enumerating Files Completed. {fileQueue.Count} Discovered.";

                pbar.ObservedError = enumeratingTimedOut;
                pbar.Finished();
            }

            Console.WriteLine();
            var doneProcessing = false;

            ProgressBarOptions options2 = new()
            {
                ForegroundColor = ConsoleColor.Yellow,
                ForegroundColorDone = ConsoleColor.DarkGreen,
                BackgroundColor = ConsoleColor.DarkGray,
                ForegroundColorError = ConsoleColor.Red,
                BackgroundCharacter = '\u2593',
                DisableBottomPercentage = false,
                ShowEstimatedDuration = true
            };
            using (ProgressBar progressBar = new(fileQueue.Count, "Analyzing Files.", options2))
            {
                Stopwatch sw = new();
                sw.Start();
                _ = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        DoProcessing(fileQueue);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Unexpected exception while processing. {Type}:{Message}",
                            e.GetType().Name, e.Message);
                    }
                    finally
                    {
                        doneProcessing = true;
                    }
                });

                while (!doneProcessing)
                {
                    Thread.Sleep(ProgressBarUpdateDelay);
                    var current = _metaDataHelper.Files.Count;
                    var timePerRecord = sw.Elapsed.TotalMilliseconds / current;
                    var millisExpected = (int)(timePerRecord * (fileQueue.Count - current));
                    TimeSpan timeExpected = new(0, 0, 0, 0, millisExpected);
                    progressBar.Tick(_metaDataHelper.Files.Count, timeExpected,
                        $"Analyzing Files. {_metaDataHelper.Matches.Count} Matches. {_metaDataHelper.Files.Count(x => x.Status == ScanState.Skipped)} Files Skipped. {_metaDataHelper.Files.Count(x => x.Status == ScanState.TimedOut)} Timed Out. {_metaDataHelper.Files.Count(x => x.Status == ScanState.Affected)} Affected. {_metaDataHelper.Files.Count(x => x.Status == ScanState.Analyzed)} Not Affected.");
                }

                // If processing timed out it will have set the status to timeout skipped.
                timedOut = _metaDataHelper.Files.Any(x => x.Status == ScanState.TimeOutSkipped);

                progressBar.Message = timedOut
                    ? $"Overall processing timeout hit. {_metaDataHelper.Matches.Count} Matches. {_metaDataHelper.Files.Count(x => x.Status == ScanState.Skipped)} Files Skipped. {_metaDataHelper.Files.Count(x => x.Status == ScanState.TimedOut)} Timed Out. {_metaDataHelper.Files.Count(x => x.Status == ScanState.Affected)} Affected. {_metaDataHelper.Files.Count(x => x.Status == ScanState.Analyzed)} Not Affected."
                    : $"{_metaDataHelper.Matches.Count} Matches. {_metaDataHelper.Files.Count(x => x.Status == ScanState.Skipped)} Files Skipped. {_metaDataHelper.Files.Count(x => x.Status == ScanState.TimedOut)} Timed Out. {_metaDataHelper.Files.Count(x => x.Status == ScanState.Affected)} Affected. {_metaDataHelper.Files.Count(x => x.Status == ScanState.Analyzed)} Not Affected.";
                progressBar.Tick(progressBar.MaxTicks);
            }

            Console.WriteLine();
        }

        //wrapup result status
        if (!_options.SuccessErrorCodeOnNoMatches)
        {
            if (!_options.NoFileMetadata && _metaDataHelper.Files.All(x => x.Status == ScanState.Skipped))
            {
                _logger.LogError(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOSUPPORTED_FILETYPES));
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
            }
            else if (!_metaDataHelper.HasFindings)
            {
                _logger.LogError(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOPATTERNS));
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
            }
        }

        _metaDataHelper.Metadata.DateScanned = DateScanned.ToString(CultureInfo.InvariantCulture);
        _metaDataHelper.PrepareReport();
        analyzeResult.Metadata = _metaDataHelper.Metadata; //replace instance with metadatahelper processed one

        if (timedOut)
        {
            _logger.LogError(MsgHelp.GetString(MsgHelp.ID.ANALYZE_PROCESSING_TIMED_OUT));
            analyzeResult.Metadata.TimedOut = true;
            analyzeResult.ResultCode = AnalyzeResult.ExitCode.TimedOut;
        }

        return analyzeResult;
    }

    /// <summary>
    ///     Do processing of the given file entries - return if the timeout was hit
    /// </summary>
    /// <param name="fileEntries"></param>
    /// <param name="cts"></param>
    /// <returns>True when the timeout was hit</returns>
    private void DoProcessing(IEnumerable<FileEntry> fileEntries, CancellationTokenSource? cts = null)
    {
        cts ??= new CancellationTokenSource();
        if (_options.ProcessingTimeOut > 0)
        {
            var t = Task.Run(() => PopulateRecords(cts.Token, fileEntries), cts.Token);
            if (!t.Wait(new TimeSpan(0, 0, 0, 0, _options.ProcessingTimeOut)))
            {
                cts.Cancel();
                // Populate skips for all the entries we didn't process
                foreach (var entry in fileEntries.Where(x => _metaDataHelper.Files.All(y => x.FullPath != y.FileName)))
                    _metaDataHelper.Files.Add(new FileRecord
                    {
                        AccessTime = entry.AccessTime, CreateTime = entry.CreateTime, ModifyTime = entry.ModifyTime,
                        FileName = entry.FullPath, Status = ScanState.TimeOutSkipped
                    });
            }
        }
        else
        {
            PopulateRecords(cts.Token, fileEntries);
        }
    }
}
