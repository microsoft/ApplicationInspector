// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInspector.Commands
{
    public class TagDiffOptions : CommandOptions
    {
        public string SourcePath1 { get; set; } = "";
        public string SourcePath2 { get; set; } = "";
        public string TestType { get; set; } = "equality";
        public string FilePathExclusions { get; set; } = "sample,example,test,docs,.vs,.git";
        public string? CustomRulesPath { get; set; }
        public bool IgnoreDefaultRules { get; set; }
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
                _options.Log ??= Utils.SetupLogging(_options);
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
            if (!Utils.CLIExecutionContext)
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

            if (_options?.SourcePath1 == _options?.SourcePath2)
            {
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.TAGDIFF_SAME_FILE_ARG));
            }
            else if (string.IsNullOrEmpty(_options?.SourcePath1) || string.IsNullOrEmpty(_options?.SourcePath2))
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

            TagDiffResult tagDiffResult = new TagDiffResult() { AppVersion = Utils.GetVersionString() };

            //save to quiet analyze cmd and restore
            WriteOnce.ConsoleVerbosity saveVerbosity = WriteOnce.Verbosity;

            try
            {
                #region setup analyze calls

                AnalyzeCommand cmd1 = new AnalyzeCommand(new AnalyzeOptions
                {
                    SourcePath = _options?.SourcePath1 ?? "",
                    CustomRulesPath = _options?.CustomRulesPath,
                    IgnoreDefaultRules = _options?.IgnoreDefaultRules ?? false,
                    FilePathExclusions = _options?.FilePathExclusions ?? "",
                    ConsoleVerbosityLevel = "none",
                    Log = _options?.Log
                });
                AnalyzeCommand cmd2 = new AnalyzeCommand(new AnalyzeOptions
                {
                    SourcePath = _options?.SourcePath2 ?? "",
                    CustomRulesPath = _options?.CustomRulesPath,
                    IgnoreDefaultRules = _options != null ? _options.IgnoreDefaultRules : false,
                    FilePathExclusions = _options?.FilePathExclusions ?? "",
                    ConsoleVerbosityLevel = "none",
                    Log = _options?.Log
                });

                AnalyzeResult analyze1 = cmd1.GetResult();
                AnalyzeResult analyze2 = cmd2.GetResult();

                //restore
                WriteOnce.Verbosity = saveVerbosity;

                #endregion setup analyze calls

                bool equalTagsCompare1;
                bool equalTagsCompare2;

                //process results for each analyze call before comparing results
                if (analyze1.ResultCode == AnalyzeResult.ExitCode.CriticalError)
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_CRITICAL_FILE_ERR, _options?.SourcePath1 ?? ""));
                }
                else if (analyze2.ResultCode == AnalyzeResult.ExitCode.CriticalError)
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_CRITICAL_FILE_ERR, _options?.SourcePath2 ?? ""));
                }
                else if (analyze1.ResultCode == AnalyzeResult.ExitCode.NoMatches || analyze2.ResultCode == AnalyzeResult.ExitCode.NoMatches)
                {
                    throw new OpException(MsgHelp.GetString(MsgHelp.ID.TAGDIFF_NO_TAGS_FOUND));
                }
                else //compare tag results; assumed (result1&2 == AnalyzeCommand.ExitCode.Success)
                {
                    int count1 = 0;
                    int sizeTags1 = analyze1.Metadata.UniqueTags != null ? analyze1.Metadata.UniqueTags.Count : 0;
                    string[] file1Tags = new string[sizeTags1];

                    foreach (string tag in analyze1.Metadata.UniqueTags ?? new List<string>())
                    {
                        file1Tags[count1++] = tag;
                    }

                    int count2 = 0;
                    int sizeTags2 = analyze2.Metadata.UniqueTags != null ? analyze2.Metadata.UniqueTags.Count : 0;
                    string[] file2Tags = new string[sizeTags2];

                    foreach (string tag in analyze2.Metadata.UniqueTags ?? new List<string>())
                    {
                        file2Tags[count2++] = tag;
                    }

                    //can't simply compare counts as content may differ; must compare both in directions in two passes a->b; b->a
                    equalTagsCompare1 = CompareTags(file1Tags, file2Tags, ref tagDiffResult, TagDiff.DiffSource.Source1);

                    //reverse order for second pass
                    equalTagsCompare2 = CompareTags(file2Tags, file1Tags, ref tagDiffResult, TagDiff.DiffSource.Source2);

                    //final results
                    bool resultsDiffer = !(equalTagsCompare1 && equalTagsCompare2);
                    if (_arg_tagTestType == TagTestType.Inequality && !resultsDiffer)
                    {
                        tagDiffResult.ResultCode = TagDiffResult.ExitCode.TestFailed;
                    }
                    else if (_arg_tagTestType == TagTestType.Equality && resultsDiffer)
                    {
                        tagDiffResult.ResultCode = TagDiffResult.ExitCode.TestFailed;
                    }
                    else
                    {
                        tagDiffResult.ResultCode = TagDiffResult.ExitCode.TestPassed;
                    }
                }
            }
            catch (OpException e)
            {
                WriteOnce.Verbosity = saveVerbosity;
                WriteOnce.Error(e.Message);
                //caught for CLI callers with final exit msg about checking log or throws for DLL callers
                throw;
            }

            return tagDiffResult;
        }

        private bool CompareTags(string[] fileTags1, string[] fileTags2, ref TagDiffResult tagDiffResult, TagDiff.DiffSource source)
        {
            bool found = true;
            //are all tags in file1 found in file2
            foreach (string s1 in fileTags1)
            {
                if (!fileTags2.Contains(s1))
                {
                    found = false;
                    tagDiffResult.TagDiffList.Add(new TagDiff() { Tag = s1, Source = source });
                }
            }

            return found;
        }
    }
}