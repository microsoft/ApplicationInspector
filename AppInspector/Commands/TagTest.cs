// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DevSkim;
using Newtonsoft.Json;

namespace Microsoft.AppInspector.CLI.Commands
{

    /// <summary>
    /// Used to test a specific set of rules were all found in target source; Pass/Fail as well as inverse option to test if a set of rules is not 
    /// found in source code
    /// </summary>
    public class TagTestCommand : ICommand
    {
        enum TagTestType { RulesPresent, RulesNotPresent}

        private string _arg_srcPath;
        private string _arg_customRulesPath;
        private string _arg_outputFile;
        private bool _arg_ignoreDefault;
        private TagTestType _arg_tagTestType;
        
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
            WriteOnce.ConsoleVerbosityLevel arg_consoleVerbosityLevel;
            Enum.TryParse(opt.ConsoleVerbosityLevel, true, out arg_consoleVerbosityLevel);
            WriteOnce.Verbosity = arg_consoleVerbosityLevel;

            if (string.IsNullOrEmpty(opt.TestType))
                _arg_tagTestType = TagTestType.RulesPresent;
            else if (!Enum.TryParse(opt.TestType, true, out _arg_tagTestType))
                throw new ArgumentException("Invalid -t argument value", opt.TestType);

            _arg_ignoreDefault = opt.IgnoreDefaultRules;
            
            if (string.IsNullOrEmpty(opt.CustomRulesPath) && _arg_ignoreDefault)
                throw new ArgumentException("--ignore-default-rules enabled, --custom-rules-path required");
        }


        public int Run()
        {
            WriteOnce.Write("TagTest command running", ConsoleColor.Cyan, WriteOnce.ConsoleVerbosityLevel.Low);
            WriteOnce.NewLine(WriteOnce.ConsoleVerbosityLevel.Low);

            //setup output                       
            TextWriter outputWriter;
            if (!string.IsNullOrEmpty(_arg_outputFile))
            {
                outputWriter = File.CreateText(_arg_outputFile);
                outputWriter.WriteLine(Program.GetVersionString());
                WriteOnce.Writer = outputWriter;
            }
         
            //init based on true or false present argument value
            bool testSuccess = false;

            RuleSet rules = new RuleSet();

            #region addrules
            //get rules from Rules subfolder to avoid having to pack into one file as a resource
            //review if want to change later...
            if (!_arg_ignoreDefault)
            {
                rules.AddDirectory(Helper.GetPath(Helper.AppPath.defaultRules));
            }

            //add custom rules paths if any specified by caller
            if (_arg_customRulesPath != null)
            {
                if (Directory.Exists(_arg_customRulesPath))
                    rules.AddDirectory(_arg_customRulesPath);
                else
                    rules.AddFile(_arg_customRulesPath);
            }

            #endregion

            //one file vs ruleset
            string tmp1 = Path.GetTempFileName();
            WriteOnce.ConsoleVerbosityLevel saveVerbosity = WriteOnce.Verbosity;
            WriteOnce.Verbosity = saveVerbosity;

            AnalyzeCommand cmd1 = new AnalyzeCommand(new AnalyzeCommandOptions
            {
                SourcePath = _arg_srcPath,
                OutputFilePath = tmp1,
                OutputFileFormat = "json",
                CustomRulesPath = _arg_customRulesPath,
                IgnoreDefaultRules = _arg_ignoreDefault,
                SimpleTagsOnly = true,
                UniqueTagsOnly = true,
                ConsoleVerbosityLevel = "Low"
            });

            AnalyzeCommand.ExitCode result = AnalyzeCommand.ExitCode.CriticalError;

            //quiet analysis commands
            WriteOnce.Verbosity = WriteOnce.ConsoleVerbosityLevel.Low;
            result = (AnalyzeCommand.ExitCode)cmd1.Run();
            WriteOnce.Verbosity = saveVerbosity;

            if (result == AnalyzeCommand.ExitCode.CriticalError)
            {
                WriteOnce.Error("Critical error analyzing source path. Check logs for more.");
                return (int)result;
            }
            else if (result == AnalyzeCommand.ExitCode.NoMatches)
            {
                WriteOnce.Any(string.Format("Tagtest for [{0}] in source: {1}", _arg_tagTestType.ToString(),
                       _arg_tagTestType == TagTestType.RulesNotPresent ? "success" : "failed"));

                return (int)result;
            }
            else //assumed (result == AnalyzeCommand.ExitCode.MatchesFound)
            {
                string file1TagsJson = File.ReadAllText(tmp1);
                var file1TagsObj = JsonConvert.DeserializeObject<TagsFile[]>(file1TagsJson);
                var file1Tags = file1TagsObj.First(); // here we have a single FileList object
                File.Delete(tmp1);

                WriteOnce.Info("TestTest Report", WriteOnce.ConsoleVerbosityLevel.High);

                bool cancel = false;
                foreach (Rule r in rules)
                {
                    //supports both directions by generalizing 
                    string[] testList1 = _arg_tagTestType == TagTestType.RulesNotPresent ?
                        r.Tags : file1Tags.Tags;

                    string[] testList2 = _arg_tagTestType == TagTestType.RulesNotPresent ?
                       file1Tags.Tags : r.Tags;

                    foreach (string t in testList2)
                    {
                        if (TagTest(testList1, t))
                        {
                            testSuccess = true;
                            WriteOnce.Any("Found " + t + " in source.", WriteOnce.ConsoleVerbosityLevel.High);
                            break;
                        }
                        else
                        {
                            testSuccess = false;
                            cancel = true;
                            WriteOnce.Any("Missing " + t + " in source.", WriteOnce.ConsoleVerbosityLevel.High);
                        }
                    }

                    if (cancel)
                        break;
                }
            }

            WriteOnce.Any(string.Format("Test for all [{0}] in source: {1}", _arg_tagTestType.ToString(),
                    testSuccess ? "passed" : "failed"), ConsoleColor.Cyan, WriteOnce.ConsoleVerbosityLevel.Low);


            WriteOnce.FlushAll();

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
