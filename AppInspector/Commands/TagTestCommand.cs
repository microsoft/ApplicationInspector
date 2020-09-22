// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInspector.Commands
{
    public class TagTestOptions : CommandOptions
    {
        public string? SourcePath { get; set; }
        public string TestType { get; set; } = "rulespresent";
        public string? CustomRulesPath { get; set; }
        public string FilePathExclusions { get; set; } = "sample,example,test,docs,.vs,.git";
    }

    public class TagStatus
    {
        /// <summary>
        /// Tag name from the applicable rule
        /// </summary>
        public string? Tag { get; set; }

        /// <summary>
        /// True/false indicator of test status for given tag
        /// </summary>
        public bool Detected { get; set; }
    }

    public class TagTestResult : Result
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
        /// List of tags from test and detection status of each
        /// </summary>
        [JsonProperty(Order = 3, PropertyName = "tagsStatusList")]
        public List<TagStatus> TagsStatusList { get; set; }

        public TagTestResult()
        {
            TagsStatusList = new List<TagStatus>();
        }
    }

    /// <summary>
    /// Used to test a specific set of rules were all found in target source; Pass/Fail as well as inverse option to test if a set of rules is not
    /// found in source code
    /// </summary>
    public class TagTestCommand
    {
        private enum TagTestType { RulesPresent, RulesNotPresent }

        private readonly TagTestOptions _options;
        private TagTestType _arg_tagTestType;
        private RuleSet _rulesSet;

        /// Compares a set of rules against a source path...
        /// Used for both RulesPresent and RulesNotePresent options
        /// Focus is pass/fail not detailed comparison output -see Tagdiff for more
        public TagTestCommand(TagTestOptions opt)
        {
            _options = opt;
            _options.TestType ??= "rulespresent";

            try
            {
                _options.Log ??= Utils.SetupLogging(_options);
                WriteOnce.Log ??= _options.Log;
                _rulesSet = new RuleSet(_options.Log);

                ConfigureConsoleOutput();
                ConfigureCompareTest();
                ConfigureRules();
            }
            catch (OpException e) //group error handling
            {
                WriteOnce.Error(e.Message);
                throw;
            }
        }

        #region configure

        /// <summary>
        /// Establish console verbosity
        /// For NuGet DLL use, console is muted overriding any arguments sent
        /// </summary>
        private void ConfigureConsoleOutput()
        {
            WriteOnce.SafeLog("TagTestCommand::ConfigureConsoleOutput", LogLevel.Trace);

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

        private void ConfigureCompareTest()
        {
            if (!Enum.TryParse(_options.TestType, true, out _arg_tagTestType))
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, _options.TestType));
            }
        }

        public void ConfigureRules()
        {
            WriteOnce.SafeLog("TagTestCommand::ConfigRules", LogLevel.Trace);

            if (string.IsNullOrEmpty(_options.CustomRulesPath))
            {
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.CMD_NORULES_SPECIFIED));
            }

            RulesVerifier verifier = new RulesVerifier(_options.CustomRulesPath, _options.Log);
            verifier.Verify();
            if (!verifier.IsVerified)
            {
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_RESULTS_FAIL));
            }

            _rulesSet = verifier.CompiledRuleset ?? new RuleSet(null);

            //error check based on ruleset not path enumeration
            if (!_rulesSet.Any())
            {
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.CMD_NORULES_SPECIFIED));
            }
        }

        #endregion configure

        /// <summary>
        /// Main entry from CLI
        /// </summary>
        /// <returns></returns>
        public TagTestResult GetResult()
        {
            WriteOnce.SafeLog("TagTestCommand::Run", LogLevel.Trace);
            WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_RUNNING, "Tag Test"));
            TagTestResult tagTestResult = new TagTestResult()
            {
                AppVersion = Utils.GetVersionString()
            };

            //init based on true or false present argument value
            WriteOnce.ConsoleVerbosity saveVerbosity = WriteOnce.Verbosity;
            AnalyzeResult.ExitCode analyzeCmdResult = AnalyzeResult.ExitCode.CriticalError;

            try
            {
                //setup analyze call with silent option
                AnalyzeCommand cmd1 = new AnalyzeCommand(new AnalyzeOptions
                {
                    SourcePath = _options?.SourcePath ?? "",
                    IgnoreDefaultRules = true,
                    CustomRulesPath = _options?.CustomRulesPath ?? "",
                    FilePathExclusions = _options?.FilePathExclusions ?? "",
                    ConsoleVerbosityLevel = "none",
                    Log = _options?.Log
                });

                //get and perform initial analyze on results
                AnalyzeResult analyze1 = cmd1.GetResult();

                //restore
                WriteOnce.Verbosity = saveVerbosity;

                if (analyze1.ResultCode == AnalyzeResult.ExitCode.CriticalError)
                {
                    throw new OpException(MsgHelp.GetString(MsgHelp.ID.CMD_CRITICAL_FILE_ERR));
                }
                else if (analyzeCmdResult == AnalyzeResult.ExitCode.NoMatches)
                {
                    WriteOnce.General(MsgHelp.FormatString(MsgHelp.ID.TAGTEST_RESULTS_TEST_TYPE, _arg_tagTestType.ToString()), false, WriteOnce.ConsoleVerbosity.Low);
                    tagTestResult.ResultCode = _arg_tagTestType == TagTestType.RulesPresent ? TagTestResult.ExitCode.TestFailed : TagTestResult.ExitCode.TestPassed;
                }
                else //assumed (result == AnalyzeCommand.ExitCode.MatchesFound)
                {
                    tagTestResult.ResultCode = TagTestResult.ExitCode.TestPassed;

                    int count = 0;
                    int sizeTags = analyze1.Metadata.UniqueTags != null ? analyze1.Metadata.UniqueTags.Count : 0;
                    string[] tagsFound = new string[sizeTags];

                    foreach (string tag in analyze1.Metadata.UniqueTags ?? new List<string>())
                    {
                        tagsFound[count++] = tag;
                    }

                    foreach (Rule? rule in _rulesSet)
                    {
                        //supports both directions by generalizing
                        string[]? testList1 = _arg_tagTestType == TagTestType.RulesNotPresent ?
                            rule?.Tags : tagsFound;

                        string[]? testList2 = _arg_tagTestType == TagTestType.RulesNotPresent ?
                            tagsFound : rule?.Tags;

                        foreach (string t in testList2 ?? new string[] { })
                        {
                            if (TagTest(testList1, t))
                            {
                                tagTestResult.TagsStatusList.Add(new TagStatus() { Tag = t, Detected = true });
                            }
                            else
                            {
                                tagTestResult.ResultCode = TagTestResult.ExitCode.TestFailed;
                                tagTestResult.TagsStatusList.Add(new TagStatus() { Tag = t, Detected = false });
                            }
                        }
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

            return tagTestResult;
        }

        private bool TagTest(string[]? list, string? test)
        {
            if (_arg_tagTestType == TagTestType.RulesNotPresent)
            {
                return (!list.Any(v => v.Equals(test)));
            }
            else
            {
                return (list.Any(v => v.Equals(test)));
            }
        }
    }
}