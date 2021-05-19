﻿// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using GlobExpressions;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Newtonsoft.Json;
using NLog;
using ShellProgressBar;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Options specific to analyze operation not to be confused with CLIAnalyzeCmdOptions which include CLI only args
    /// </summary>
    public class AnalyzeOptions : CommandOptions
    {
        public IEnumerable<string> SourcePath { get; set; } = new string[0];
        public string? CustomRulesPath { get; set; }
        public bool IgnoreDefaultRules { get; set; }
        public string ConfidenceFilters { get; set; } = "high,medium";
        public string FilePathExclusions { get; set; } = "none";
        public bool SingleThread { get; set; } = false;
        public bool TreatEverythingAsCode { get; set; } = false;
        public bool NoShowProgress { get; set; } = true;
        public int FileTimeOut { get; set; } = 0;
        public int ProcessingTimeOut { get; set; } = 0;
        public int ContextLines { get; set; } = 3;
        public bool ScanUnknownTypes { get; set; }
    }

    /// <summary>
    /// Result of Analyze command GetResult() operation
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

        [JsonProperty(Order = 2, PropertyName = "resultCode")]
        public ExitCode ResultCode { get; set; }

        /// <summary>
        /// Analyze command result object containing scan properties
        /// </summary>
        [JsonProperty(Order = 3, PropertyName = "metaData")]
        public MetaData Metadata { get; set; }

        public AnalyzeResult()
        {
            Metadata = new MetaData("", "");//needed for serialization for other commands; replaced later
        }
    }

    /// <summary>
    /// Analyze operation for setup and processing of results from Rulesengine
    /// </summary>
    public class AnalyzeCommand
    {
        private List<string> _srcfileList = new List<string>();
        private MetaDataHelper? _metaDataHelper; //wrapper containing MetaData object to be assigned to result
        private RuleProcessor? _rulesProcessor;

        private DateTime DateScanned { get; set; }

        private readonly List<Glob> _fileExclusionList = new();
        private Confidence _confidence;
        private readonly AnalyzeOptions _options; //copy of incoming caller options

        public AnalyzeCommand(AnalyzeOptions opt)
        {
            _options = opt;

            if (!string.IsNullOrEmpty(opt.FilePathExclusions) && opt.FilePathExclusions != "none")
            {
                _fileExclusionList = opt.FilePathExclusions.Split(",").Select(x => new Glob(x)).ToList();
            }
            else
            {
                _fileExclusionList = new List<Glob>();
            }

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

            if (!_options.SourcePath.Any())
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_REQUIRED_ARG_MISSING, "SourcePath"));
            }

            foreach(var entry in _options.SourcePath)
            {
                if (Directory.Exists(entry))
                {
                    try
                    {
                        _srcfileList.AddRange(Directory.EnumerateFiles(entry, "*.*", SearchOption.AllDirectories));
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                else if (File.Exists(entry))
                {
                    _srcfileList.Add(entry);
                }
                else
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, string.Join(',', _options.SourcePath)));
                }
            }
            if (!_srcfileList.Any())
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, string.Join(',', _options.SourcePath)));
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
                treatEverythingAsCode = _options.TreatEverythingAsCode,
                confidenceFilter = _confidence,
                Parallel = !_options.SingleThread
            };

            _rulesProcessor = new RuleProcessor(rulesSet, rpo);

            //create metadata helper to wrap and help populate metadata from scan
            _metaDataHelper = new MetaDataHelper(string.Join(',',_options.SourcePath));
        }

        #endregion configureMethods

        /// <summary>
        /// Populate the MetaDataHelper with the data from the FileEntries.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="opts"></param>
        /// <param name="populatedEntries"></param>
        public AnalyzeResult.ExitCode PopulateRecords(CancellationToken cancellationToken, AnalyzeOptions opts, IEnumerable<FileEntry> populatedEntries)
        {
            WriteOnce.SafeLog("AnalyzeCommand::PopulateRecords", LogLevel.Trace);

            if (_rulesProcessor is null || populatedEntries is null)
            {
                return AnalyzeResult.ExitCode.CriticalError;
            }

            if (opts.SingleThread)
            {
                foreach (var entry in populatedEntries)
                {
                    if (cancellationToken.IsCancellationRequested) { return AnalyzeResult.ExitCode.Canceled; }
                    ProcessAndAddToMetadata(entry);
                }
            }
            else
            {
                try
                {
                    Parallel.ForEach(populatedEntries, new ParallelOptions() { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount * 3 / 2 }, entry => ProcessAndAddToMetadata(entry));
                }
                catch (OperationCanceledException)
                {
                    return AnalyzeResult.ExitCode.Canceled;
                }
            }

            return AnalyzeResult.ExitCode.Success;

            void ProcessAndAddToMetadata(FileEntry file)
            {
                var fileRecord = new FileRecord() { FileName = file.FullPath, ModifyTime = file.ModifyTime, CreateTime = file.CreateTime, AccessTime = file.AccessTime };

                var sw = new Stopwatch();
                sw.Start();

                if (_fileExclusionList.Any(x => x.IsMatch(file.FullPath)))
                {
                    WriteOnce.SafeLog(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_EXCLUDED_TYPE_SKIPPED, fileRecord.FileName), LogLevel.Debug);
                    fileRecord.Status = ScanState.Skipped;
                }
                else
                {
                    if (IsBinary(file.Content))
                    {
                        WriteOnce.SafeLog(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_EXCLUDED_BINARY, fileRecord.FileName), LogLevel.Debug);
                        fileRecord.Status = ScanState.Skipped;
                    }
                    else
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
                            if (!_options.ScanUnknownTypes)
                            {
                                fileRecord.Status = ScanState.Skipped;
                            }
                        }

                        if (fileRecord.Status != ScanState.Skipped)
                        {
                            List<MatchRecord> results = new List<MatchRecord>();

                            if (opts.FileTimeOut > 0)
                            {
                                using var cts = new CancellationTokenSource();
                                var t = Task.Run(() => results = _rulesProcessor.AnalyzeFile(file, languageInfo, null, opts.ContextLines), cts.Token);
                                if (!t.Wait(new TimeSpan(0, 0, 0, 0, opts.FileTimeOut)))
                                {
                                    WriteOnce.Error($"{file.FullPath} timed out.");
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
                                results = _rulesProcessor.AnalyzeFile(file, languageInfo, null, opts.ContextLines);
                                fileRecord.Status = ScanState.Analyzed;
                            }

                            if (results.Any())
                            {
                                fileRecord.Status = ScanState.Affected;
                                fileRecord.NumFindings = results.Count;
                            }
                            foreach (var matchRecord in results)
                            {
                                _metaDataHelper?.AddMatchRecord(matchRecord);
                            }
                        }
                    }
                }

                sw.Stop();

                fileRecord.ScanTime = sw.Elapsed;

                _metaDataHelper?.Files.Add(fileRecord);
            }
        }

        public async Task<AnalyzeResult.ExitCode> PopulateRecordsAsync(CancellationToken cancellationToken, AnalyzeOptions opts)
        {
            WriteOnce.SafeLog("AnalyzeCommand::PopulateRecordsAsync", LogLevel.Trace);

            if (_rulesProcessor is null)
            {
                return AnalyzeResult.ExitCode.CriticalError;
            }
            await foreach (var entry in GetFileEntriesAsync(opts))
            {
                if (cancellationToken.IsCancellationRequested) { return AnalyzeResult.ExitCode.Canceled; }
                await ProcessAndAddToMetadata(entry, cancellationToken);
            }

            return AnalyzeResult.ExitCode.Success;

            async Task ProcessAndAddToMetadata(FileEntry file, CancellationToken cancellationToken)
            {
                var fileRecord = new FileRecord() { FileName = file.FullPath, ModifyTime = file.ModifyTime, CreateTime = file.CreateTime, AccessTime = file.AccessTime };

                var sw = new Stopwatch();
                sw.Start();

                if (_fileExclusionList.Any(x => x.IsMatch(file.FullPath)))
                {
                    WriteOnce.SafeLog(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_EXCLUDED_TYPE_SKIPPED, fileRecord.FileName), LogLevel.Debug);
                    fileRecord.Status = ScanState.Skipped;
                }
                else
                {
                    if (IsBinary(file.Content))
                    {
                        WriteOnce.SafeLog(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_EXCLUDED_BINARY, fileRecord.FileName), LogLevel.Debug);
                        fileRecord.Status = ScanState.Skipped;
                    }
                    else
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
                            if (!opts.ScanUnknownTypes)
                            {
                                fileRecord.Status = ScanState.Skipped;
                            }
                        }

                        if (fileRecord.Status != ScanState.Skipped)
                        {
                            var results = await _rulesProcessor.AnalyzeFileAsync(file, languageInfo, cancellationToken, null, opts.ContextLines);
                            fileRecord.Status = ScanState.Analyzed;

                            if (results.Any())
                            {
                                fileRecord.Status = ScanState.Affected;
                                fileRecord.NumFindings = results.Count;
                            }
                            foreach (var matchRecord in results)
                            {
                                _metaDataHelper?.AddMatchRecord(matchRecord);
                            }
                        }
                    }
                }

                sw.Stop();

                fileRecord.ScanTime = sw.Elapsed;

                _metaDataHelper?.Files.Add(fileRecord);
            }
        }

        public IEnumerable<FileEntry> GetFileEntries()
        {
            WriteOnce.SafeLog("AnalyzeCommand::GetFileEntries", LogLevel.Trace);

            Extractor extractor = new();

            foreach (var srcFile in _srcfileList ?? new List<string>())
            {
                foreach(var entry in extractor.Extract(srcFile, new ExtractorOptions() { Parallel = false }))
                {
                    yield return entry;
                }
            }
        }


        public async IAsyncEnumerable<FileEntry> GetFileEntriesAsync(AnalyzeOptions opts)
        {
            WriteOnce.SafeLog("AnalyzeCommand::GetFileEntriesAsync", LogLevel.Trace);

            Extractor extractor = new();
            foreach (var srcFile in _srcfileList ?? new List<string>())
            {
                await foreach (var entry in extractor.ExtractAsync(srcFile, new ExtractorOptions() { Parallel = false }))
                {
                    yield return entry;
                }
            }
        }


        // Follows Perl's model, if there are NULs or too many non printable characters, this is probably a binary file
        private bool IsBinary(Stream fileContents)
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
                    else if (char.IsControl(ch) && !char.IsWhiteSpace(ch))
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

        public async Task<AnalyzeResult> GetResultAsync(CancellationToken cancellationToken)
        {
            WriteOnce.SafeLog("AnalyzeCommand::GetResultAsync", LogLevel.Trace);
            WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_RUNNING, "Analyze"));
            AnalyzeResult analyzeResult = new AnalyzeResult()
            {
                AppVersion = Utils.GetVersionString()
            };

            var exitCode = await PopulateRecordsAsync(cancellationToken, _options);

            //wrapup result status
            if (_metaDataHelper?.Files.All(x => x.Status == ScanState.Skipped) ?? false)
            {
                WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOSUPPORTED_FILETYPES));
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
            }
            else if (_metaDataHelper?.Matches.Count == 0)
            {
                WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOPATTERNS));
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
            }
            else if (_metaDataHelper != null && _metaDataHelper.Metadata != null)
            {
                _metaDataHelper.Metadata.DateScanned = DateScanned.ToString();
                _metaDataHelper.PrepareReport();
                analyzeResult.Metadata = _metaDataHelper.Metadata; //replace instance with metadatahelper processed one
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.Success;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.Canceled;
            }

            return analyzeResult;
        }

        /// <summary>
        /// Main entry point to start analysis from CLI; handles setting up rules, directory enumeration
        /// file type detection and handoff
        /// Pre: All Configure Methods have been called already and we are ready to SCAN
        /// </summary>
        /// <returns></returns>
        public AnalyzeResult GetResult()
        {
            WriteOnce.SafeLog("AnalyzeCommand::GetResult", LogLevel.Trace);
            WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_RUNNING, "Analyze"));
            AnalyzeResult analyzeResult = new AnalyzeResult()
            {
                AppVersion = Utils.GetVersionString()
            };

            var timedOut = false;

            if (!_options.NoShowProgress)
            {
                var done = false;
                ConcurrentBag<FileEntry> fileQueue = new();

                _ = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        foreach (var entry in GetFileEntries())
                        {
                            fileQueue.Add(entry);
                        }
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
                        Thread.Sleep(100);
                        pbar.Message = $"Enumerating Files. {fileQueue.Count} Discovered.";
                    }
                    pbar.Message = $"Enumerating Files. {fileQueue.Count} Discovered.";

                    pbar.Finished();
                }
                Console.Write(Environment.NewLine);
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
                        DoProcessing(fileQueue);
                        done = true;
                    });

                    while (!done)
                    {
                        Thread.Sleep(100);
                        var current = _metaDataHelper?.Files.Count ?? 0;
                        var timePerRecord = sw.Elapsed.TotalMilliseconds / current;
                        var millisExpected = (int)(timePerRecord * (fileQueue.Count - current));
                        var timeExpected = new TimeSpan(0, 0, 0, 0, millisExpected);
                        progressBar.Tick(_metaDataHelper?.Files.Count ?? 0, timeExpected, $"Analyzing Files. {_metaDataHelper?.Matches.Count} Matches. {_metaDataHelper?.Files.Count(x => x.Status == ScanState.Skipped)} Files Skipped. {_metaDataHelper?.Files.Count(x => x.Status == ScanState.TimedOut)} Timed Out. {_metaDataHelper?.Files.Count(x => x.Status == ScanState.Affected)} Affected. {_metaDataHelper?.Files.Count(x => x.Status == ScanState.Analyzed)} Not Affected.");
                    }

                    progressBar.Message = $"{_metaDataHelper?.Matches.Count} Matches. {_metaDataHelper?.Files.Count(x => x.Status == ScanState.Skipped)} Files Skipped. {_metaDataHelper?.Files.Count(x => x.Status == ScanState.TimedOut)} Timed Out. {_metaDataHelper?.Files.Count(x => x.Status == ScanState.Affected)} Affected. {_metaDataHelper?.Files.Count(x => x.Status == ScanState.Analyzed)} Not Affected.";
                    progressBar.Tick(progressBar.MaxTicks);
                }
                Console.Write(Environment.NewLine);
            }
            else
            {
                DoProcessing(GetFileEntries());
            }

            //wrapup result status
            if (_metaDataHelper?.Files.All(x => x.Status == ScanState.Skipped) ?? false)
            {
                WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOSUPPORTED_FILETYPES));
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
            }
            else if (_metaDataHelper?.Matches.Count == 0)
            {
                WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOPATTERNS));
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
            }
            else if (_metaDataHelper != null && _metaDataHelper.Metadata != null)
            {
                _metaDataHelper.Metadata.DateScanned = DateScanned.ToString();
                _metaDataHelper.PrepareReport();
                analyzeResult.Metadata = _metaDataHelper.Metadata; //replace instance with metadatahelper processed one
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.Success;
            }

            if (timedOut)
            {
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.TimedOut;
            }

            return analyzeResult;

            void DoProcessing(IEnumerable<FileEntry> fileEntries)
            {
                if (_options.ProcessingTimeOut > 0)
                {
                    using var cts = new CancellationTokenSource();
                    var t = Task.Run(() => PopulateRecords(cts.Token, _options, fileEntries), cts.Token);
                    if (!t.Wait(new TimeSpan(0, 0, 0, 0, _options.ProcessingTimeOut)))
                    {
                        timedOut = true;
                        WriteOnce.Error($"Processing timed out.");
                        cts.Cancel();
                        if (_metaDataHelper is not null)
                        {
                            // Populate skips for all the entries we didn't process
                            foreach (var entry in fileEntries.Where(x => !_metaDataHelper.Files.Any(y => x.FullPath == y.FileName)))
                            {
                                _metaDataHelper.Files.Add(new FileRecord() { AccessTime = entry.AccessTime, CreateTime = entry.CreateTime, ModifyTime = entry.ModifyTime, FileName = entry.FullPath, Status = ScanState.TimeOutSkipped });
                            }
                        }
                    }
                }
                else
                {
                    PopulateRecords(new CancellationToken(), _options, fileEntries);
                }
            }
        }
    }
}