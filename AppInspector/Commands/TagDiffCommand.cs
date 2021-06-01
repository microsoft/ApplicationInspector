// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.Common;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInspector.Commands
{
    public class TagDiffOptions : LogOptions
    {
        public IEnumerable<string> SourcePath1 { get; set; } = Array.Empty<string>();
        public IEnumerable<string> SourcePath2 { get; set; } = Array.Empty<string>();
        public string TestType { get; set; } = "equality";
        public IEnumerable<string> FilePathExclusions { get; set; } = new string[] { };
        public string? CustomRulesPath { get; set; }
        public bool IgnoreDefaultRules { get; set; }
        public int FileTimeOut { get; set; }
        public int ProcessingTimeOut { get; set; }
        public bool ScanUnknownTypes { get; set; }
        public bool SingleThread { get; set; }
        public string ConfidenceFilters { get; set; } = "high,medium";
    }

    /// <summary>
    /// Contains a tag that was detected missing in source1 or source2
    /// </summary>
    public class TagDiff
    {
        public enum DiffSource
        {
            Source1 = 1,
            Source2 = 2
        }

        /// <summary>
        /// Tag value from rule used in comparison
        /// </summary>
        [JsonProperty(PropertyName = "tag")]
        public string? Tag { get; set; }

        /// <summary>
        /// Source file (src1/src2) from the command option arguments
        /// </summary>
        [JsonProperty(PropertyName = "source")]
        public DiffSource Source { get; set; }
    }

    /// <summary>
    /// Result wrapping list of tags not found in one of the sources scanned
    /// </summary>
    public class TagDiffResult : Result
    {
        public enum ExitCode
        {
            TestPassed = 0,
            TestFailed = 1,
            CriticalError = Utils.ExitCode.CriticalError //ensure common value for final exit log mention
        }

        [JsonProperty(Order = 2, PropertyName = "resultCode")]
        public ExitCode ResultCode { get; set; }

        /// <summary>
        /// List of tags which differ between src1 and src2
        /// </summary>
        [JsonProperty(Order = 3, PropertyName = "tagDiffList")]
        public List<TagDiff> TagDiffList;

        public TagDiffResult()
        {
            TagDiffList = new List<TagDiff>();
        }
    }

    /// <summary>
    /// Used to compare two source paths and report tag differences
    /// </summary>
    public class TagDiffCommand
    {
        private enum TagTestType { Equality, Inequality }

        private readonly TagDiffOptions? _options;
        private TagTestType _arg_tagTestType;

        public TagDiffCommand(TagDiffOptions opt)
        {
            _options = opt;
            _options.TestType ??= "equality";

            try
            {
                _options.Log ??= Common.Utils.SetupLogging(_options);
                WriteOnce.Log ??= _options.Log;

                ConfigureConsoleOutput();
                ConfigureCompareType();
                ConfigSourceToScan();
            }
            catch (OpException e) //group error handling
            {
                WriteOnce.Error(e.Message);
                throw;
            }
        }

        #region config

        /// <summary>
        /// Establish console verbosity
        /// For NuGet DLL use, console is muted overriding any arguments sent
        /// Pre: Always call again after ConfigureFileOutput
        /// </summary>
        private void ConfigureConsoleOutput()
        {
            WriteOnce.SafeLog("TagDiffCommand::ConfigureConsoleOutput", LogLevel.Trace);

            //Set console verbosity based on run context (none for DLL use) and caller arguments
            if (!Common.Utils.CLIExecutionContext)
            {
                WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.None;
            }
            else
            {
                WriteOnce.ConsoleVerbosity verbosity = WriteOnce.ConsoleVerbosity.Medium;
                if (!Enum.TryParse(_options?.ConsoleVerbosityLevel, true, out verbosity))
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-x"));
                }
                else
                {
                    WriteOnce.Verbosity = verbosity;
                }
            }
        }

        private void ConfigureCompareType()
        {
            if (!Enum.TryParse(_options?.TestType, true, out _arg_tagTestType))
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, _options?.TestType ?? ""));
            }
        }

        private void ConfigSourceToScan()
        {
            WriteOnce.SafeLog("TagDiff::ConfigRules", LogLevel.Trace);

            if ((!_options?.SourcePath1.Any() ?? true) || (!_options?.SourcePath2.Any() ?? true))
            {
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.CMD_INVALID_ARG_VALUE));
            }
        }

        #endregion config

        /// <summary>
        /// Main entry from CLI
        /// </summary>
        /// <returns></returns>
        public TagDiffResult GetResult()
        {
            WriteOnce.SafeLog("TagDiffCommand::Run", LogLevel.Trace);
            WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_RUNNING, "Tag Diff"));

            TagDiffResult tagDiffResult = new TagDiffResult() { AppVersion = Common.Utils.GetVersionString() };

            //save to quiet analyze cmd and restore
            WriteOnce.ConsoleVerbosity saveVerbosity = WriteOnce.Verbosity;

            try
            {
                if (_options is null)
                {
                    throw new ArgumentNullException("_options");
                }
                AnalyzeCommand cmd1 = new(new AnalyzeOptions()
                {
                    SourcePath = _options.SourcePath1,
                    CustomRulesPath = _options.CustomRulesPath,
                    IgnoreDefaultRules = _options.IgnoreDefaultRules,
                    FilePathExclusions = _options.FilePathExclusions,
                    ConsoleVerbosityLevel = "none",
                    Log = _options.Log,
                    TagsOnly = true,
                    ConfidenceFilters = _options.ConfidenceFilters,
                    FileTimeOut = _options.FileTimeOut,
                    ProcessingTimeOut = _options.ProcessingTimeOut,
                    NoFileMetadata = true,
                    NoShowProgress = true,
                    ScanUnknownTypes = _options.ScanUnknownTypes,
                    SingleThread = _options.SingleThread,
                });
                AnalyzeCommand cmd2 = new(new AnalyzeOptions()
                {
                    SourcePath = _options.SourcePath2,
                    CustomRulesPath = _options.CustomRulesPath,
                    IgnoreDefaultRules = _options.IgnoreDefaultRules,
                    FilePathExclusions = _options.FilePathExclusions,
                    ConsoleVerbosityLevel = "none",
                    Log = _options.Log,
                    TagsOnly = true,
                    ConfidenceFilters = _options.ConfidenceFilters,
                    FileTimeOut = _options.FileTimeOut,
                    ProcessingTimeOut = _options.ProcessingTimeOut,
                    NoFileMetadata = true,
                    NoShowProgress = true,
                    ScanUnknownTypes = _options.ScanUnknownTypes,
                    SingleThread = _options.SingleThread,
                });

                AnalyzeResult analyze1 = cmd1.GetResult();
                AnalyzeResult analyze2 = cmd2.GetResult();

                //restore
                WriteOnce.Verbosity = saveVerbosity;

                //process results for each analyze call before comparing results
                if (analyze1.ResultCode == AnalyzeResult.ExitCode.CriticalError)
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_CRITICAL_FILE_ERR, string.Join(',',_options.SourcePath1)));
                }
                else if (analyze2.ResultCode == AnalyzeResult.ExitCode.CriticalError)
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_CRITICAL_FILE_ERR, string.Join(',', _options.SourcePath2)));
                }
                else if (analyze1.ResultCode == AnalyzeResult.ExitCode.NoMatches || analyze2.ResultCode == AnalyzeResult.ExitCode.NoMatches)
                {
                    throw new OpException(MsgHelp.GetString(MsgHelp.ID.TAGDIFF_NO_TAGS_FOUND));
                }
                else //compare tag results; assumed (result1&2 == AnalyzeCommand.ExitCode.Success)
                {
                    
                    var list1 = analyze1.Metadata.UniqueTags ?? new List<string>();
                    var list2 = analyze2.Metadata.UniqueTags ?? new List<string>();

                    var removed = list1.Except(list2);
                    var added = list2.Except(list1);

                    foreach(var add in added)
                    {
                        tagDiffResult.TagDiffList.Add(new TagDiff()
                        {
                            Source = TagDiff.DiffSource.Source2,
                            Tag = add
                        });
                    }
                    foreach (var remove in removed)
                    {
                        tagDiffResult.TagDiffList.Add(new TagDiff()
                        {
                            Source = TagDiff.DiffSource.Source1,
                            Tag = remove
                        });
                    }


                    if (tagDiffResult.TagDiffList.Count > 0)
                    {
                        tagDiffResult.ResultCode = _arg_tagTestType == TagTestType.Inequality ? TagDiffResult.ExitCode.TestPassed : TagDiffResult.ExitCode.TestFailed;
                    }
                    else
                    {
                        tagDiffResult.ResultCode = _arg_tagTestType == TagTestType.Inequality ? TagDiffResult.ExitCode.TestFailed : TagDiffResult.ExitCode.TestPassed;
                    }

                    return tagDiffResult;
                }
            }
            catch (OpException e)
            {
                WriteOnce.Verbosity = saveVerbosity;
                WriteOnce.Error(e.Message);
                //caught for CLI callers with final exit msg about checking log or throws for DLL callers
                throw;
            }
        }
    }
}