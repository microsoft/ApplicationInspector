// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.ApplicationInspector.Commands
{

    /// <summary>
    /// Used to test a specific set of rules were all found in target source; Pass/Fail as well as inverse option to test if a set of rules is not 
    /// found in source code
    /// </summary>
    public class TagTestCommand : Command
    {
        enum TagTestType { RulesPresent, RulesNotPresent }

        private string _arg_srcPath;
        private string _arg_customRulesPath;
        private string _arg_outputFile;
        private TagTestType _arg_tagTestType;
        private RuleSet _rulesSet;
        private string _arg_consoleVerbosityLevel;

        public enum ExitCode
        {
            NoDiff = 0,
            DiffFound = 1,
            CriticalError = 2
        }


        /// Compares a set of rules against a source path...
        /// Used for both RulesPresent and RulesNotePresent options
        /// Focus is pass/fail not detailed comparison output -see Tagdiff for more
        public TagTestCommand(TagTestCommandOptions opt)
        {
            _arg_srcPath = opt.SourcePath;
            _arg_customRulesPath = opt.CustomRulesPath;
            _arg_outputFile = opt.OutputFilePath;
            _arg_consoleVerbosityLevel = opt.ConsoleVerbosityLevel ?? "medium";
            opt.TestType ??= "RulesPresent";
            _arg_logger = opt.Log;

            WriteOnce.ConsoleVerbosity verbosity = WriteOnce.ConsoleVerbosity.Medium;
            if (!Enum.TryParse(_arg_consoleVerbosityLevel, true, out verbosity))
                throw new OpException(String.Format(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-x")));
            WriteOnce.Verbosity = verbosity;

            if (!Enum.TryParse(opt.TestType, true, out _arg_tagTestType))
                throw new OpException(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, opt.TestType));

            _rulesSet = new RuleSet(_arg_logger);

            if (string.IsNullOrEmpty(opt.CustomRulesPath))
                throw new OpException(ErrMsg.GetString(ErrMsg.ID.CMD_NORULES_SPECIFIED));

            ConfigureRules();
            ConfigureOutput();
        }


        public void ConfigureRules()
        {
            List<string> rulePaths = new List<string>();

            if (!string.IsNullOrEmpty(_arg_customRulesPath))
                rulePaths.Add(_arg_customRulesPath);

            foreach (string rulePath in rulePaths)
            {
                if (Directory.Exists(rulePath))
                    _rulesSet.AddDirectory(rulePath);
                else if (File.Exists(rulePath))
                    _rulesSet.AddFile(rulePath);
                else
                {
                    throw new OpException(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_RULE_PATH, rulePath));
                }
            }

            //error check based on ruleset not path enumeration
            if (_rulesSet.Count() == 0)
            {
                throw new OpException(ErrMsg.GetString(ErrMsg.ID.CMD_NORULES_SPECIFIED));
            }

        }


        void ConfigureOutput()
        {
            //setup output                       
            TextWriter outputWriter;
            if (!string.IsNullOrEmpty(_arg_outputFile))
            {
                outputWriter = File.CreateText(_arg_outputFile);
                outputWriter.WriteLine(Utils.GetVersionString());
                WriteOnce.Writer = outputWriter;
            }
        }



        /// <summary>
        /// Option for DLL use as alternate to Run which only outputs a file to return results as string
        /// CommandOption defaults will not have been set when used as DLL via CLI processing so some checks added
        /// </summary>
        /// <returns>output results</returns>
        public string GetResult()
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            if (!assembly.GetName().Name.Contains("ApplicationInspector.CLI"))
            {
                WriteOnce.FlushAll();
                WriteOnce.Log = _arg_logger;
            }

            _arg_outputFile ??= "output.txt";

            if ((int)ExitCode.CriticalError != Run())
            {
                return File.ReadAllText(_arg_outputFile);
            }

            return string.Empty;
        }


        /// <summary>
        /// Main entry from CLI
        /// </summary>
        /// <returns></returns>
        public override int Run()
        {
            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_RUNNING, "tagtest"));

            //init based on true or false present argument value
            bool testSuccess = true;

            //one file vs ruleset
            string tmp1 = Path.GetTempFileName();
            WriteOnce.ConsoleVerbosity saveVerbosity = WriteOnce.Verbosity;
            AnalyzeCommand.ExitCode result = AnalyzeCommand.ExitCode.CriticalError;

            //setup analyze call with silent option
            #region analyzesetup
            try
            {
                AnalyzeCommand cmd1 = new AnalyzeCommand(new AnalyzeCommandOptions
                {
                    SourcePath = _arg_srcPath,
                    OutputFilePath = tmp1,
                    OutputFileFormat = "json",
                    IgnoreDefaultRules = true,
                    CustomRulesPath = _arg_customRulesPath,
                    SimpleTagsOnly = true,
                    ConsoleVerbosityLevel = "None",
                    Log = _arg_logger
                });


                //quiet analysis commands
                result = (AnalyzeCommand.ExitCode)cmd1.Run();
            }
            catch (Exception e)
            {
                WriteOnce.Verbosity = saveVerbosity;
                throw e;
            }

            //restore
            WriteOnce.Verbosity = saveVerbosity;

            if (result == AnalyzeCommand.ExitCode.CriticalError)
            {
                throw new OpException(ErrMsg.GetString(ErrMsg.ID.CMD_CRITICAL_FILE_ERR));
            }
            else if (result == AnalyzeCommand.ExitCode.NoMatches)
            {
                //results
                WriteOnce.General(ErrMsg.FormatString(ErrMsg.ID.TAGTEST_RESULTS_TEST_TYPE, _arg_tagTestType.ToString()), false, WriteOnce.ConsoleVerbosity.Low);
                if (_arg_tagTestType == TagTestType.RulesPresent)
                    WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.TAGTEST_RESULTS_FAIL), true, ConsoleColor.Red, WriteOnce.ConsoleVerbosity.Low);
                else
                    WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.TAGTEST_RESULTS_SUCCESS), true, ConsoleColor.Green, WriteOnce.ConsoleVerbosity.Low);

                WriteOnce.FlushAll();
                WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "Tagtest"));

                return (int)ExitCode.CriticalError;
            }

            #endregion

            //assumed (result == AnalyzeCommand.ExitCode.MatchesFound)
            string file1TagsJson = File.ReadAllText(tmp1);
            var file1Tags = JsonConvert.DeserializeObject<TagsFile>(file1TagsJson);
            File.Delete(tmp1);

            foreach (Rule r in _rulesSet)
            {
                //supports both directions by generalizing 
                string[] testList1 = _arg_tagTestType == TagTestType.RulesNotPresent ?
                    r.Tags : file1Tags.Tags;

                string[] testList2 = _arg_tagTestType == TagTestType.RulesNotPresent ?
                    file1Tags.Tags : r.Tags;

                foreach (string t in testList2)
                {
                    if (TagTest(testList1, t))
                        WriteOnce.Result(ErrMsg.FormatString(ErrMsg.ID.TAGTEST_RESULTS_TAGS_FOUND, t), true, WriteOnce.ConsoleVerbosity.High);
                    else
                    {
                        testSuccess = false;
                        WriteOnce.Result(ErrMsg.FormatString(ErrMsg.ID.TAGTEST_RESULTS_TAGS_MISSING, t), true, WriteOnce.ConsoleVerbosity.High);
                    }
                }
            }

            //results
            WriteOnce.General(ErrMsg.FormatString(ErrMsg.ID.TAGTEST_RESULTS_TEST_TYPE, _arg_tagTestType.ToString()), false, WriteOnce.ConsoleVerbosity.Low);
            if (testSuccess)
                WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.TAGTEST_RESULTS_SUCCESS), true, ConsoleColor.Green, WriteOnce.ConsoleVerbosity.Low);
            else
                WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.TAGTEST_RESULTS_FAIL), true, ConsoleColor.Red, WriteOnce.ConsoleVerbosity.Low);

            WriteOnce.FlushAll();
            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "Tagtest"));

            return testSuccess ? (int)ExitCode.NoDiff : (int)ExitCode.DiffFound;
        }


        bool TagTest(string[] list, string test)
        {
            if (_arg_tagTestType == TagTestType.RulesNotPresent)
                return (!list.Any(v => v.Equals(test)));
            else
                return (list.Any(v => v.Equals(test)));
        }
    }
}
