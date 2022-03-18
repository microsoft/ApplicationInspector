// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.Commands
{
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class TagDiffOptions
    {
        public IEnumerable<string> SourcePath1 { get; set; } = Array.Empty<string>();
        public IEnumerable<string> SourcePath2 { get; set; } = Array.Empty<string>();
        public string TestType { get; set; } = "equality";
        public IEnumerable<string> FilePathExclusions { get; set; } = Array.Empty<string>();
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
        private readonly ILoggerFactory? _factory;
        private readonly ILogger<TagDiffCommand> _logger;
        private TagTestType _arg_tagTestType;

        public TagDiffCommand(TagDiffOptions opt, ILoggerFactory? loggerFactory = null)
        {
            _options = opt;
            _options.TestType ??= "equality";
            _factory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<TagDiffCommand>() ?? NullLogger<TagDiffCommand>.Instance;
            try
            {
                ConfigureCompareType();
                ConfigSourceToScan();
            }
            catch (OpException e) //group error handling
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        #region config

        private void ConfigureCompareType()
        {
            if (!Enum.TryParse(_options?.TestType, true, out _arg_tagTestType))
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, _options?.TestType ?? ""));
            }
        }

        private void ConfigSourceToScan()
        {
            _logger.LogTrace("TagDiff::ConfigRules");

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
            _logger.LogTrace("TagDiffCommand::Run");
            _logger.LogInformation(MsgHelp.GetString(MsgHelp.ID.CMD_RUNNING), "Tag Diff");

            TagDiffResult tagDiffResult = new() { AppVersion = Common.Utils.GetVersionString() };

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
                    TagsOnly = true,
                    ConfidenceFilters = _options.ConfidenceFilters,
                    FileTimeOut = _options.FileTimeOut,
                    ProcessingTimeOut = _options.ProcessingTimeOut,
                    NoFileMetadata = true,
                    NoShowProgress = true,
                    ScanUnknownTypes = _options.ScanUnknownTypes,
                    SingleThread = _options.SingleThread,
                }, _factory);
                AnalyzeCommand cmd2 = new(new AnalyzeOptions()
                {
                    SourcePath = _options.SourcePath2,
                    CustomRulesPath = _options.CustomRulesPath,
                    IgnoreDefaultRules = _options.IgnoreDefaultRules,
                    FilePathExclusions = _options.FilePathExclusions,
                    TagsOnly = true,
                    ConfidenceFilters = _options.ConfidenceFilters,
                    FileTimeOut = _options.FileTimeOut,
                    ProcessingTimeOut = _options.ProcessingTimeOut,
                    NoFileMetadata = true,
                    NoShowProgress = true,
                    ScanUnknownTypes = _options.ScanUnknownTypes,
                    SingleThread = _options.SingleThread,
                }, _factory);

                AnalyzeResult analyze1 = cmd1.GetResult();
                AnalyzeResult analyze2 = cmd2.GetResult();

                //process results for each analyze call before comparing results
                if (analyze1.ResultCode == AnalyzeResult.ExitCode.CriticalError)
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_CRITICAL_FILE_ERR, string.Join(',', _options.SourcePath1)));
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

                    foreach (var add in added)
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
                _logger.LogError(e.Message);
                //caught for CLI callers with final exit msg about checking log or throws for DLL callers
                throw;
            }
        }
    }
}