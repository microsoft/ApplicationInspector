// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace Microsoft.AppInspector.Commands
{
    [Serializable]
    public class TagsFile
    {
        [JsonProperty(PropertyName = "tags")]
        public string[] Tags { get; set; }
    }



    /// <summary>
    /// Used to compare two source paths and report tag differences
    /// </summary>
    public class TagDiffCommand : ICommand
   {
        private string _arg_src1, _arg_src2;
        private string _arg_rulesPath;
        private string _arg_outputFile;
        private bool _arg_ignoreDefault;
        private TagTestType _arg_tagTestType;

        public enum ExitCode
        {
            NoDiff = 0,
            DiffFound = 1,
            CriticalError = 2
        }

        enum TagTestType { Equality, Inequality }

        public TagDiffCommand(TagDiffCommandOptions opt)
        {
            _arg_src1 = opt.SourcePath1;
            _arg_src2 = opt.SourcePath2;
            _arg_rulesPath = opt.CustomRulesPath;
            _arg_outputFile = opt.OutputFilePath;
            WriteOnce.ConsoleVerbosityLevel arg_consoleVerbosityLevel;
            Enum.TryParse(opt.ConsoleVerbosityLevel, true, out arg_consoleVerbosityLevel);
            WriteOnce.Verbosity = arg_consoleVerbosityLevel;

            if (!Enum.TryParse(opt.TestType, true, out _arg_tagTestType))
                throw new ArgumentException("Invalid -t argument value", opt.TestType);

            _arg_ignoreDefault = opt.IgnoreDefaultRules;

        }

        public int Run()
        {
            WriteOnce.Write("TagDiff command running\n", ConsoleColor.Cyan, WriteOnce.ConsoleVerbosityLevel.Low);


            //setup output                       
            TextWriter outputWriter;
            if (!string.IsNullOrEmpty(_arg_outputFile))
            {
                outputWriter = File.CreateText(_arg_outputFile);
                outputWriter.WriteLine(Program.GetVersionString());
                WriteOnce.Writer = outputWriter;
            }

            if (_arg_src1 == _arg_src2)
            {
                WriteOnce.Error("Same file passed in for both sources. Test terminated.");
                return (int)ExitCode.CriticalError;
            }
            else if (string.IsNullOrEmpty(_arg_src1) || string.IsNullOrEmpty(_arg_src2))
            {
                WriteOnce.Error("Required [path1] or [path2] argument missing.");
                return (int)ExitCode.CriticalError;
            }

            //save to quiet analyze cmd
            WriteOnce.ConsoleVerbosityLevel saveVerbosity = WriteOnce.Verbosity;

            string tmp1 = Path.GetTempFileName();
            string tmp2 = Path.GetTempFileName();
            AnalyzeCommand cmd1 = new AnalyzeCommand(new AnalyzeCommandOptions { SourcePath = _arg_src1,
                OutputFilePath = tmp1,
                OutputFileFormat = "json",
                CustomRulesPath = _arg_rulesPath,
                IgnoreDefaultRules = _arg_ignoreDefault,
                SimpleTagsOnly = true,
                UniqueTagsOnly = true,
                ConsoleVerbosityLevel = "Low" });
            AnalyzeCommand cmd2 = new AnalyzeCommand(new AnalyzeCommandOptions
            {
                SourcePath = _arg_src2,
                OutputFilePath = tmp2,
                OutputFileFormat = "json",
                CustomRulesPath = _arg_rulesPath,
                IgnoreDefaultRules = _arg_ignoreDefault,
                SimpleTagsOnly = true,
                UniqueTagsOnly = true,
                ConsoleVerbosityLevel = "Low"
            });

            //quiet analysis commands
            WriteOnce.Verbosity = WriteOnce.ConsoleVerbosityLevel.Low;
            AnalyzeCommand.ExitCode result1 = AnalyzeCommand.ExitCode.CriticalError;
            AnalyzeCommand.ExitCode result2 = AnalyzeCommand.ExitCode.CriticalError;
            result1 = (AnalyzeCommand.ExitCode)cmd1.Run();
            result2 = (AnalyzeCommand.ExitCode)cmd2.Run();
            WriteOnce.Verbosity = saveVerbosity;


            bool equal1 = true;
            bool equal2 = true;

            if (result1 == AnalyzeCommand.ExitCode.CriticalError)
            {
                WriteOnce.Error("Critical error processing file " + _arg_src1 + ".  Check path.");
                return (int)ExitCode.CriticalError;
            }
            else if (result2 == AnalyzeCommand.ExitCode.CriticalError)
            {
                WriteOnce.Error("Critical error processing file " + _arg_src2 + ".  Check path.");
                return (int)ExitCode.CriticalError;
            }
            else if (result1 == AnalyzeCommand.ExitCode.NoMatches || result2 == AnalyzeCommand.ExitCode.NoMatches)
            {
                WriteOnce.Error("No tags found in one or both source paths");
                return (int)ExitCode.CriticalError;
            }
            else //assumed (result1&2 == AnalyzeCommand.ExitCode.MatchesFound)
            {
                string file1TagsJson = File.ReadAllText(tmp1);
                string file2TagsJson = File.ReadAllText(tmp2);

                var file1Tags = JsonConvert.DeserializeObject<TagsFile[]>(file1TagsJson).First();
                var file2Tags = JsonConvert.DeserializeObject<TagsFile[]>(file2TagsJson).First();

                WriteOnce.Info("TagDiff Report");

                //can't simply compare counts as content may differ; must compare both in directions

                //first pass
                WriteOnce.Any("[Tags in " + Path.GetFileName(_arg_src1) + " not detected in " + Path.GetFileName(_arg_src2) + "]",
                        ConsoleColor.White, WriteOnce.ConsoleVerbosityLevel.High);
                equal1 = CompareTags(file1Tags.Tags, file2Tags.Tags);
                
                //reverse order for second pass
                WriteOnce.Any("[Tags in " + Path.GetFileName(_arg_src2) + " not detected in " + Path.GetFileName(_arg_src1) + "]",
                        ConsoleColor.White, WriteOnce.ConsoleVerbosityLevel.High);
                equal2 = CompareTags(file2Tags.Tags, file1Tags.Tags);

                //final
                WriteOnce.Any(string.Format("Files were {0} to contain differences.",
                       equal1 && equal2 ? "not found" : "found"), ConsoleColor.Cyan, WriteOnce.ConsoleVerbosityLevel.Low);

                if (_arg_tagTestType == TagTestType.Inequality)
                {
                    WriteOnce.Any(string.Format("Test for all [{0}] in source: {1}", _arg_tagTestType.ToString(),
                        equal1 && equal2 ? "failed" : "passed"), (equal1 && equal2) ? ConsoleColor.Red : ConsoleColor.Green);
                }
                else
                {
                    WriteOnce.Any(string.Format("Test for all [{0}] in source: {1}", _arg_tagTestType.ToString(),
                        equal1 && equal2 ? "passed" : "failed"), (equal1 && equal2) ? ConsoleColor.Green : ConsoleColor.Red);
                }

                WriteOnce.Info("Report completed");

            }

            //cleanup

            try 
            {
                File.Delete(tmp1);
                File.Delete(tmp2);
            }
            catch
            {
                //no action needed; 
            }

            WriteOnce.FlushAll();

            return equal1 && equal2 ? (int)ExitCode.NoDiff : (int)ExitCode.DiffFound;
        }



        bool CompareTags(string[] fileTags1, string[] fileTags2)
        {
            bool found = true;
            //are all tags in file1 found in file2
            foreach (string s1 in fileTags1)
            {
                if (!fileTags2.Contains(s1))
                {
                    found = false;
                    WriteOnce.Any(s1, ConsoleColor.Yellow, WriteOnce.ConsoleVerbosityLevel.High);
                }
            }

            //none missing
            if (found)
                WriteOnce.Any("None", ConsoleColor.Yellow, WriteOnce.ConsoleVerbosityLevel.High);

            return found;
        }


    }
}
