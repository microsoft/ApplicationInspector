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
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ShellProgressBar;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Options specific to analyze operation not to be confused with CLIAnalyzeCmdOptions which include CLI only args
    /// </summary>
    public class AnalyzeOptions : CommandOptions
    {
        public string SourcePath { get; set; } = "";
        public string? CustomRulesPath { get; set; }
        public bool IgnoreDefaultRules { get; set; }
        public bool AllowDupTags { get; set; }
        public string MatchDepth { get; set; } = "best";
        public string ConfidenceFilters { get; set; } = "high,medium";
        public string FilePathExclusions { get; set; } = "sample,example,test,docs,.vs,.git";
        public bool SingleThread { get; set; } = false;
        public bool TreatEverythingAsCode { get; set; } = false;
        public bool NoShowProgress { get; set; } = true;
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
            CriticalError = Utils.ExitCode.CriticalError //ensure common value for final exit log mention
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
        private readonly int WARN_ZIP_FILE_SIZE = 1024 * 1000 * 10;  // warning for large zip files
        private readonly int MAX_FILESIZE = 1024 * 1000 * 5;  // Skip source files larger than 5 MB and log

        private IEnumerable<string>? _srcfileList;
        private MetaDataHelper? _metaDataHelper; //wrapper containing MetaData object to be assigned to result
        private RuleProcessor? _rulesProcessor;

        private DateTime DateScanned { get; set; }

        private DateTime _lastUpdated;

        public MetaData MetaData { get { return _metaDataHelper.Metadata; } }

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
        private readonly AnalyzeOptions _options; //copy of incoming caller options

        public AnalyzeCommand(AnalyzeOptions opt)
        {
            _options = opt;
            _options.MatchDepth ??= "best";

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
            _rulesProcessor = new RuleProcessor(rulesSet, _confidence, _options.Log, !_options.AllowDupTags, _options.MatchDepth == "first", treatEverythingAsCode: _options.TreatEverythingAsCode);
            _rulesProcessor.UniqueTagExceptions = "Metric.,Dependency.".Split(",");//fix to enable non-unique tags if metric counter related

            //create metadata helper to wrap and help populate metadata from scan
            _metaDataHelper = new MetaDataHelper(_options.SourcePath, !_options.AllowDupTags);
        }

        #endregion configureMethods

        public void PopulateRecords(CancellationToken cancellationToken, AnalyzeOptions opts)
        {
            WriteOnce.SafeLog("AnalyzeCommand::EnumerateRecords", LogLevel.Trace);

            var analyzeResult = new AnalyzeResult();
            if (_rulesProcessor is null)
            {
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.CriticalError;
                return;
            }

            Extractor extractor = new();

            foreach (var srcFile in _srcfileList ?? Array.Empty<string>())
            {
                if (cancellationToken.IsCancellationRequested) { break; }
                else if (opts.SingleThread)
                {
                    foreach (var file in extractor.Extract(srcFile))
                    {
                        if (cancellationToken.IsCancellationRequested) { break; }
                        ProcessAndAddToMetadata(file);
                    }
                }
                else
                {
                    Parallel.ForEach(extractor.Extract(srcFile), new ParallelOptions() { CancellationToken = cancellationToken }, file =>
                    {
                        ProcessAndAddToMetadata(file);
                    });
                }
            }

            void ProcessAndAddToMetadata(FileEntry file)
            {
                _metaDataHelper?.Metadata.IncrementTotalFiles();

                LanguageInfo languageInfo = new LanguageInfo();

                if (ChecksPassed(file, ref languageInfo))
                {
                    _ = _metaDataHelper?.FileExtensions.TryAdd(Path.GetExtension(file.FullPath).Replace('.', ' ').TrimStart(), 0);

                    if (Language.FromFileName(file.FullPath, ref languageInfo))
                    {
                        _metaDataHelper?.AddLanguage(languageInfo.Name);
                    }
                    else
                    {
                        _metaDataHelper?.AddLanguage("Unknown");
                        languageInfo = new LanguageInfo() { Extensions = new string[] { Path.GetExtension(file.FullPath) }, Name = "Unknown" };
                    }

                    _metaDataHelper?.Metadata.IncrementFilesAnalyzed();

                    var results = _rulesProcessor.AnalyzeFile(file, languageInfo, opts.AllowDupTags ? null : _metaDataHelper?.Metadata.UniqueTags);

                    if (results.Any())
                    {
                        _metaDataHelper?.Metadata.IncrementFilesAffected();
                    }
                    foreach (var matchRecord in results)
                    {
                        _metaDataHelper?.AddMatchRecord(matchRecord);
                    }
                }
                else
                {
                    _metaDataHelper?.Metadata.IncrementFilesSkipped();
                }
            }
        }

    

        /// <summary>
        /// Main entry point to start analysis from CLI; handles setting up rules, directory enumeration
        /// file type detection and handoff
        /// Pre: All Configure Methods have been called already and we are ready to SCAN
        /// </summary>
        /// <returns></returns>
        public AnalyzeResult GetResult()
        {
            WriteOnce.SafeLog("AnalyzeCommand::Run", LogLevel.Trace);
            WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_RUNNING, "Analyze"));
            AnalyzeResult analyzeResult = new AnalyzeResult()
            {
                AppVersion = Utils.GetVersionString()
            };



            if (!_options.NoShowProgress)
            {
                var done = false;
                _ = Task.Factory.StartNew(() =>
                {
                    PopulateRecords(new CancellationToken(), _options);
                    done = true;
                });
                var options = new ProgressBarOptions
                {
                    ForegroundColor = ConsoleColor.Yellow,
                    ForegroundColorDone = ConsoleColor.DarkGreen,
                    BackgroundColor = ConsoleColor.DarkGray,
                    BackgroundCharacter = '\u2593'
                };

                using var pbar = new IndeterminateProgressBar("Indeterminate", options);
                pbar.Message = $"Analyzing Records";

                while (!done)
                {
                    pbar.Message = $"Enumerating and Analyzing Files. {_metaDataHelper?.Metadata.TotalMatchesCount} findings in {_metaDataHelper?.Metadata.FilesAnalyzed} files.";
                    Thread.Sleep(10);
                }

                pbar.Finished();
            }
            else
            {
                PopulateRecords(new CancellationToken(), _options);
            }

            //wrapup result status
            if (_metaDataHelper?.Metadata.TotalFiles == _metaDataHelper?.Metadata.FilesSkipped)
            {
                WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOSUPPORTED_FILETYPES));
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
            }
            else if (_metaDataHelper?.Metadata?.Matches?.Count == 0)
            {
                WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOPATTERNS));
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
            }
            else if (_metaDataHelper != null && _metaDataHelper.Metadata != null)
            {
                _metaDataHelper.Metadata.LastUpdated = LastUpdated.ToString();
                _metaDataHelper.Metadata.DateScanned = DateScanned.ToString();
                _metaDataHelper.PrepareReport();
                analyzeResult.Metadata = _metaDataHelper.Metadata; //replace instance with metadatahelper processed one
                analyzeResult.ResultCode = AnalyzeResult.ExitCode.Success;
            }

            return analyzeResult;
        }

        /// <summary>
        /// Common validation called by ProcessAsFile and UnzipAndProcess to ensure same order and checks made
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="languageInfo"></param>
        /// <param name="fileLength">should be > zero if called from unzip method</param>
        /// <returns></returns>
        private bool ChecksPassed(FileEntry fileEntry, ref LanguageInfo languageInfo, long fileLength = 0)
        {
            // 1. Check for exclusions
            if (ExcludeFileFromScan(fileEntry.FullPath))
            {
                return false;
            }

            // 2. Skip if exceeds file size limits
            if (fileLength > MAX_FILESIZE)
            {
                WriteOnce.SafeLog(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_FILESIZE_SKIPPED, fileEntry.FullPath), LogLevel.Warn);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Allow callers to exclude files that are not core code files and may otherwise report false positives for matches
        /// Does not apply to root scan folder which may be named .\test etc. but to subdirectories only
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool ExcludeFileFromScan(string filePath)
        {
            string? rootScanDirectory = Directory.Exists(_options.SourcePath) ? _options.SourcePath : Path.GetDirectoryName(_options.SourcePath);
            bool scanningRootFolder = !string.IsNullOrEmpty(filePath) && Path.GetDirectoryName(filePath)?.ToLower() == rootScanDirectory?.ToLower();
            // 2. Skip excluded files i.e. sample, test or similar from sub-directories (not root #210) unless ignore filter requested
            if (!scanningRootFolder)
            {
                if (_fileExclusionList != null && _fileExclusionList.Any(v => filePath.ToLower().Contains(v)))
                {
                    WriteOnce.SafeLog(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_EXCLUDED_TYPE_SKIPPED, filePath??""), LogLevel.Warn);
                    return true;
                }
            }

            return false;
        }
    }
}