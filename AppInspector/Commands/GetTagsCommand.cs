// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using DotLiquid.Tags;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShellProgressBar;
using System.Diagnostics;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Options specific to analyze operation not to be confused with CLIAnalyzeCmdOptions which include CLI only args
    /// </summary>
    public class GetTagsCommandOptions : CommandOptions
    {
        public string SourcePath { get; set; } = "";
        public string? CustomRulesPath { get; set; }
        public bool IgnoreDefaultRules { get; set; }
        public string ConfidenceFilters { get; set; } = "high,medium";
        public string FilePathExclusions { get; set; } = "sample,example,test,docs,.vs,.git";
        public bool SingleThread { get; set; } = false;
        public bool TreatEverythingAsCode { get; set; } = false;
        public bool NoShowProgress { get; set; } = true;
        public int FileTimeOut { get; set; } = 0;
    }

    /// <summary>
    /// Result of Analyze command GetResult() operation
    /// </summary>
    public class GetTagsResult : Result
    {
        public enum ExitCode
        {
            Success = 0,
            NoMatches = 1,
            CriticalError = Utils.ExitCode.CriticalError //ensure common value for final exit log mention
        }

        [JsonProperty(Order = 2, PropertyName = "resultCode")]
        public ExitCode ResultCode { get; set; }

        /// <summary>
        /// Analyze command result object containing scan properties
        /// </summary>
        [JsonProperty(Order = 3, PropertyName = "metaData")]
        public MetaData Metadata { get; set; }

        public GetTagsResult()
        {
            Metadata = new MetaData("", "");//needed for serialization for other commands; replaced later
        }
    }

    /// <summary>
    /// Analyze operation for setup and processing of results from Rulesengine
    /// </summary>
    public class GetTagsCommand
    {
        private readonly int MAX_FILESIZE = 1024 * 1000 * 5;  // Skip source files larger than 5 MB and log

        private IEnumerable<string>? _srcfileList;
        private MetaDataHelper? _metaDataHelper; //wrapper containing MetaData object to be assigned to result
        private RuleProcessor? _rulesProcessor;

        private DateTime DateScanned { get; set; }

        private DateTime _lastUpdated;

        public MetaData? MetaData { get { return _metaDataHelper?.Metadata; } }

        /// <summary>
        /// Updated dynamically to more recent file in source
        /// </summary>
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set
            {
                //find last updated file in solution
                if (_lastUpdated < value)
                {
                    _lastUpdated = value;
                }
            }
        }

        private readonly List<string>? _fileExclusionList;
        private Confidence _confidence;
        private readonly GetTagsCommandOptions _options; //copy of incoming caller options

        public GetTagsCommand(GetTagsCommandOptions opt)
        {
            _options = opt;

            if (!string.IsNullOrEmpty(opt.FilePathExclusions))
            {
                _fileExclusionList = opt.FilePathExclusions.ToLower().Split(",").ToList();
                if (_fileExclusionList != null && (_fileExclusionList.Contains("none") || _fileExclusionList.Contains("None")))
                {
                    _fileExclusionList.Clear();
                }
            }

            LastUpdated = DateTime.MinValue;
            DateScanned = DateTime.Now;

            try
            {
                _options.Log ??= Utils.SetupLogging(_options);
                WriteOnce.Log ??= _options.Log;

                ConfigureConsoleOutput();
                ConfigSourcetoScan();
                ConfigConfidenceFilters();
                ConfigRules();
            }
            catch (OpException e) //group error handling
            {
                WriteOnce.Error(e.Message);
                throw;
            }
        }

        #region configureMethods

        /// <summary>
        /// Establish console verbosity
        /// For NuGet DLL use, console is automatically muted overriding any arguments sent
        /// </summary>
        private void ConfigureConsoleOutput()
        {
            WriteOnce.SafeLog("AnalyzeCommand::ConfigureConsoleOutput", LogLevel.Trace);

            //Set console verbosity based on run context (none for DLL use) and caller arguments
            if (!Utils.CLIExecutionContext)
            {
                WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.None;
            }
            else
            {
                WriteOnce.ConsoleVerbosity verbosity = WriteOnce.ConsoleVerbosity.Medium;
                if (!Enum.TryParse(_options.ConsoleVerbosityLevel, true, out verbosity))
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-x"));
                }
                else
                {
                    WriteOnce.Verbosity = verbosity;
                }
            }
        }

        /// <summary>
        /// Expects user to supply all that apply impacting which rule pattern matches are returned
        /// </summary>
        private void ConfigConfidenceFilters()
        {
            WriteOnce.SafeLog("AnalyzeCommand::ConfigConfidenceFilters", LogLevel.Trace);
            //parse and verify confidence values
            if (string.IsNullOrEmpty(_options.ConfidenceFilters))
            {
                _confidence = Confidence.High | Confidence.Medium; //excludes low by default
            }
            else
            {
                string[] confidences = _options.ConfidenceFilters.Split(',');
                foreach (string confidence in confidences)
                {
                    Confidence single;
                    if (Enum.TryParse(confidence, true, out single))
                    {
                        _confidence |= single;
                    }
                    else
                    {
                        throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "x"));
                    }
                }
            }
        }

        /// <summary>
        /// Simple validation on source path provided for scanning and preparation
        /// </summary>
        private void ConfigSourcetoScan()
        {
            WriteOnce.SafeLog("AnalyzeCommand::ConfigSourcetoScan", LogLevel.Trace);

            if (string.IsNullOrEmpty(_options.SourcePath))
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_REQUIRED_ARG_MISSING, "SourcePath"));
            }

            if (Directory.Exists(_options.SourcePath))
            {
                try
                {
                    _srcfileList = Directory.EnumerateFiles(_options.SourcePath, "*.*", SearchOption.AllDirectories);
                    if (!_srcfileList.Any())
                    {
                        throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, _options.SourcePath));
                    }
                }
                catch (Exception)
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, _options.SourcePath));
                }
            }
            else if (File.Exists(_options.SourcePath)) //not a directory but make one for single flow
            {
                _srcfileList = new List<string>() { _options.SourcePath };
            }
            else
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, _options.SourcePath));
            }
        }

        /// <summary>
        /// Add default and/or custom rules paths
        /// Iterate paths and add to ruleset
        /// </summary>
        private void ConfigRules()
        {
            WriteOnce.SafeLog("AnalyzeCommand::ConfigRules", LogLevel.Trace);

            RuleSet? rulesSet = null;

            if (!_options.IgnoreDefaultRules)
            {
                rulesSet = Utils.GetDefaultRuleSet(_options.Log);
            }

            if (!string.IsNullOrEmpty(_options.CustomRulesPath))
            {
                if (rulesSet == null)
                {
                    rulesSet = new RuleSet(_options.Log);
                }

                if (Directory.Exists(_options.CustomRulesPath))
                {
                    rulesSet.AddDirectory(_options.CustomRulesPath);
                }
                else if (File.Exists(_options.CustomRulesPath)) //verify custom rules before use
                {
                    RulesVerifier verifier = new RulesVerifier(_options.CustomRulesPath, _options.Log);
                    verifier.Verify();
                    if (!verifier.IsVerified)
                    {
                        throw new OpException(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULE_LOADFILE_FAILED, _options.CustomRulesPath));
                    }

                    rulesSet.AddRange(verifier.CompiledRuleset);
                }
                else
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_RULE_PATH, _options.CustomRulesPath));
                }
            }

            //error check based on ruleset not path enumeration
            if (rulesSet == null || !rulesSet.Any())
            {
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.CMD_NORULES_SPECIFIED));
            }

            //instantiate a RuleProcessor with the added rules and exception for dependency
            var rpo = new RuleProcessorOptions()
            {
                logger = _options.Log,
                uniqueMatches = true,
                treatEverythingAsCode = _options.TreatEverythingAsCode,
                confidenceFilter = _confidence
            };

            _rulesProcessor = new RuleProcessor(rulesSet, rpo);

            //create metadata helper to wrap and help populate metadata from scan
            _metaDataHelper = new MetaDataHelper(_options.SourcePath, false);
        }

        #endregion configureMethods

        public IEnumerable<FileEntry> GetFileEntries(GetTagsCommandOptions opts)
        {
            WriteOnce.SafeLog("GetTagsCommand::GetFileEntries", LogLevel.Trace);

            Extractor extractor = new();

            foreach (var srcFile in _srcfileList ?? Array.Empty<string>())
            {
                foreach (var file in extractor.Extract(srcFile, new ExtractorOptions() { Parallel = false }))
                {
                    yield return file;
                }
            }
        }

        public GetTagsResult.ExitCode PopulateRecords(CancellationToken cancellationToken, GetTagsCommandOptions opts, IEnumerable<FileEntry> populatedEntries)
        {
            WriteOnce.SafeLog("GetTagsCommand::PopulateRecords", LogLevel.Trace);

            if (_rulesProcessor is null || populatedEntries is null)
            {
                return GetTagsResult.ExitCode.CriticalError;
            }
            if (opts.SingleThread)
            {
                foreach(var entry in populatedEntries)
                {
                    if (cancellationToken.IsCancellationRequested) { break; }
                    ProcessAndAddToMetadata(entry);
                }
            }
            else
            {
                Parallel.ForEach(populatedEntries, new ParallelOptions() { CancellationToken = cancellationToken }, entry => ProcessAndAddToMetadata(entry));
            }
            return GetTagsResult.ExitCode.Success;

            void ProcessAndAddToMetadata(FileEntry file)
            {
                var fileRecord = new FileRecord() { FileName = file.FullPath };

                var sw = new Stopwatch();
                sw.Start();


                if (!_options.FilePathExclusions.Any(v => file.FullPath.ToLower().Contains(v)))
                {
                    _ = _metaDataHelper?.FileExtensions.TryAdd(Path.GetExtension(file.FullPath).Replace('.', ' ').TrimStart(), 0);

                    LanguageInfo languageInfo = new LanguageInfo();

                    if (Language.FromFileName(file.FullPath, ref languageInfo))
                    {
                        _metaDataHelper?.AddLanguage(languageInfo.Name);
                    }
                    else
                    {
                        _metaDataHelper?.AddLanguage("Unknown");
                        languageInfo = new LanguageInfo() { Extensions = new string[] { Path.GetExtension(file.FullPath) }, Name = "Unknown" };
                    }

                    List<MatchRecord> results = new List<MatchRecord>();

                    if (opts.FileTimeOut > 0)
                    {
                        using var cts = new CancellationTokenSource();
                        var t = Task.Run(() => results = _rulesProcessor.AnalyzeFile(file, languageInfo, null), cts.Token);
                        if (!t.Wait(new TimeSpan(0, 0, opts.FileTimeOut)))
                        {
                            WriteOnce.Error($"{file.FullPath} analysis timed out.");
                            fileRecord.Status = ScanState.TimedOut;
                            cts.Cancel();
                        }
                        else
                        {
                            fileRecord.Status = ScanState.Analyzed;
                        }
                    }
                    else
                    {
                        results = _rulesProcessor.AnalyzeFile(file, languageInfo, null);
                        fileRecord.Status = ScanState.Analyzed;
                    }

                    if (results.Any())
                    {
                        fileRecord.Status = ScanState.Affected;
                        foreach (var matchRecord in results)
                        {
                            _metaDataHelper?.AddTagsFromMatchRecord(matchRecord);
                        }
                    }
                }
                else
                {
                    WriteOnce.SafeLog(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_EXCLUDED_TYPE_SKIPPED, fileRecord.FileName), LogLevel.Debug);
                    fileRecord.Status = ScanState.Skipped;
                }

                sw.Stop();

                fileRecord.ScanTime = sw.Elapsed;

                _metaDataHelper?.Files.Add(fileRecord);
            }
        }

    

        /// <summary>
        /// Main entry point to start analysis from CLI; handles setting up rules, directory enumeration
        /// file type detection and handoff
        /// Pre: All Configure Methods have been called already and we are ready to SCAN
        /// </summary>
        /// <returns></returns>
        public GetTagsResult GetResult()
        {
            WriteOnce.SafeLog("GetTagsCommand::Run", LogLevel.Trace);
            WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_RUNNING, "GetTags"));
            GetTagsResult getTagsResult = new GetTagsResult()
            {
                AppVersion = Utils.GetVersionString()
            };

            if (!_options.NoShowProgress)
            {
                var done = false;
                List<FileEntry> fileQueue = new ();

                _ = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        fileQueue.AddRange(GetFileEntries(_options));
                    }
                    catch (OverflowException e)
                    {
                        WriteOnce.Error($"Overflowed while extracting file entries. Check the input for quines or zip bombs. {e.Message}");
                    }
                    done = true;
                });

                var options = new ProgressBarOptions
                {
                    ForegroundColor = ConsoleColor.Yellow,
                    ForegroundColorDone = ConsoleColor.DarkGreen,
                    BackgroundColor = ConsoleColor.DarkGray,
                    BackgroundCharacter = '\u2593',
                    DisableBottomPercentage = true
                };

                using (var pbar = new IndeterminateProgressBar("Enumerating Files.", options))
                {
                    while (!done)
                    {
                        Thread.Sleep(10);
                        pbar.Message = $"Enumerating Files. {fileQueue.Count} Discovered.";
                    }
                    pbar.Message = $"Enumerating Files. {fileQueue.Count} Discovered.";
                    pbar.Finished();
                }

                done = false;

                var options2 = new ProgressBarOptions
                {
                    ForegroundColor = ConsoleColor.Yellow,
                    ForegroundColorDone = ConsoleColor.DarkGreen,
                    BackgroundColor = ConsoleColor.DarkGray,
                    BackgroundCharacter = '\u2593',
                    DisableBottomPercentage = false,
                    ShowEstimatedDuration = true
                };

                using (var progressBar = new ProgressBar(fileQueue.Count, $"Analyzing Files.", options2))
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    _ = Task.Factory.StartNew(() =>
                    {
                        PopulateRecords(new CancellationToken(), _options, fileQueue);
                        done = true;
                    });

                    while (!done)
                    {
                        Thread.Sleep(10);
                        var current = _metaDataHelper?.Files.Count ?? 0;
                        var timePerRecord = sw.Elapsed.TotalMilliseconds / current;
                        var millisExpected = (int)(timePerRecord * (fileQueue.Count - current));
                        var timeExpected = new TimeSpan(0, 0, 0, 0, millisExpected);
                        progressBar.Tick(_metaDataHelper?.Files.Count ?? 0, timeExpected, $"Analyzing Files. {_metaDataHelper?.UniqueTagsCount} Tags Found.");
                    }
                    progressBar.Message = $"Analyzing Files. {_metaDataHelper?.UniqueTagsCount} Tags Found.";
                    progressBar.Tick(progressBar.MaxTicks);
                }
            }
            else
            {
                PopulateRecords(new CancellationToken(), _options, GetFileEntries(_options));
            }

            //wrapup result status
            if (_metaDataHelper?.Files.All(x => x.Status == ScanState.Skipped) ?? false)
            {
                WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOSUPPORTED_FILETYPES));
                getTagsResult.ResultCode = GetTagsResult.ExitCode.NoMatches;
            }
            else if (_metaDataHelper?.UniqueTagsCount == 0)
            {
                WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOPATTERNS));
                getTagsResult.ResultCode = GetTagsResult.ExitCode.NoMatches;
            }
            else if (_metaDataHelper != null && _metaDataHelper.Metadata != null)
            {
                _metaDataHelper.Metadata.LastUpdated = LastUpdated.ToString();
                _metaDataHelper.Metadata.DateScanned = DateScanned.ToString();
                _metaDataHelper.PrepareReport();
                getTagsResult.Metadata = _metaDataHelper.Metadata; //replace instance with metadatahelper processed one
                getTagsResult.ResultCode = GetTagsResult.ExitCode.Success;
            }

            return getTagsResult;
        }
    }
}