// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Linq;
using System.IO;


namespace Microsoft.AppInspector
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
        private WriteOnce.ConsoleVerbosity _arg_consoleVerbosityLevel;

        public enum ExitCode
        {
            TestPassed = 0,
            TestFailed = 1,
            CriticalError = 2
        }

        enum TagTestType { Equality, Inequality }

        public TagDiffCommand(TagDiffCommandOptions opt)
        {
            _arg_src1 = opt.SourcePath1;
            _arg_src2 = opt.SourcePath2;
            _arg_rulesPath = opt.CustomRulesPath;
            _arg_outputFile = opt.OutputFilePath;
            if (!Enum.TryParse(opt.ConsoleVerbosityLevel, true, out _arg_consoleVerbosityLevel))
                throw new OpException(String.Format(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-x")));
            WriteOnce.Verbosity = _arg_consoleVerbosityLevel;

            if (!Enum.TryParse(opt.TestType, true, out _arg_tagTestType))
                throw new OpException(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, opt.TestType));

            _arg_ignoreDefault = opt.IgnoreDefaultRules;

        }

        public int Run()
        {
            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_RUNNING, "Tagdiff"));
            
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
                throw new OpException(ErrMsg.GetString(ErrMsg.ID.TAGDIFF_SAME_FILE_ARG));
            }
            else if (string.IsNullOrEmpty(_arg_src1) || string.IsNullOrEmpty(_arg_src2))
            {
                throw new OpException(ErrMsg.GetString(ErrMsg.ID.CMD_INVALID_ARG_VALUE));
            }

            #region setup analyze calls

            //save to quiet analyze cmd
            WriteOnce.ConsoleVerbosity saveVerbosity = WriteOnce.Verbosity;

            string tmp1 = Path.GetTempFileName();
            string tmp2 = Path.GetTempFileName();

            AnalyzeCommand.ExitCode result1 = AnalyzeCommand.ExitCode.CriticalError;
            AnalyzeCommand.ExitCode result2 = AnalyzeCommand.ExitCode.CriticalError;

            try
            {
                AnalyzeCommand cmd1 = new AnalyzeCommand(new AnalyzeCommandOptions
                {
                    SourcePath = _arg_src1,
                    OutputFilePath = tmp1,
                    OutputFileFormat = "json",
                    CustomRulesPath = _arg_rulesPath,
                    IgnoreDefaultRules = _arg_ignoreDefault,
                    SimpleTagsOnly = true,
                    AllowDupTags = false,
                    ConsoleVerbosityLevel = "None"
                });
                AnalyzeCommand cmd2 = new AnalyzeCommand(new AnalyzeCommandOptions
                {
                    SourcePath = _arg_src2,
                    OutputFilePath = tmp2,
                    OutputFileFormat = "json",
                    CustomRulesPath = _arg_rulesPath,
                    IgnoreDefaultRules = _arg_ignoreDefault,
                    SimpleTagsOnly = true,
                    AllowDupTags = false,
                    ConsoleVerbosityLevel = "None"
                });


                result1 = (AnalyzeCommand.ExitCode)cmd1.Run();
                result2 = (AnalyzeCommand.ExitCode)cmd2.Run();
            }
            catch (Exception e)
            {
                //restore
                WriteOnce.Verbosity = saveVerbosity;
                throw e;
            }

            //restore
            WriteOnce.Verbosity = saveVerbosity;

            #endregion

            bool successResult;
            bool equal1 = true;
            bool equal2 = true;

            //process results for each analyze call before comparing results
            if (result1 == AnalyzeCommand.ExitCode.CriticalError)
            {
                throw new OpException(ErrMsg.FormatString(ErrMsg.ID.CMD_CRITICAL_FILE_ERR, _arg_src1));
            }
            else if (result2 == AnalyzeCommand.ExitCode.CriticalError)
            {
                throw new OpException(ErrMsg.FormatString(ErrMsg.ID.CMD_CRITICAL_FILE_ERR, _arg_src2));
            }
            else if (result1 == AnalyzeCommand.ExitCode.NoMatches || result2 == AnalyzeCommand.ExitCode.NoMatches)
            {
                throw new OpException(ErrMsg.GetString(ErrMsg.ID.TAGDIFF_NO_TAGS_FOUND));
            }
            else //compare tag results; assumed (result1&2 == AnalyzeCommand.ExitCode.MatchesFound)
            {
                string file1TagsJson = File.ReadAllText(tmp1);
                string file2TagsJson = File.ReadAllText(tmp2);

                //fix #97 adjust for app version in structure change
                file1TagsJson = file1TagsJson.Substring(file1TagsJson.IndexOf("["));
                file2TagsJson = file2TagsJson.Substring(file2TagsJson.IndexOf("["));

                var file1Tags = JsonConvert.DeserializeObject<TagsFile[]>(file1TagsJson).First();
                var file2Tags = JsonConvert.DeserializeObject<TagsFile[]>(file2TagsJson).First();

                //can't simply compare counts as content may differ; must compare both in directions in two passes a->b; b->a
                //first pass
                WriteOnce.General(ErrMsg.FormatString(ErrMsg.ID.TAGDIFF_RESULTS_GAP, Path.GetFileName(_arg_src1), Path.GetFileName(_arg_src2)),
                        true, WriteOnce.ConsoleVerbosity.High);
                equal1 = CompareTags(file1Tags.Tags, file2Tags.Tags);

                //reverse order for second pass
                WriteOnce.General(ErrMsg.FormatString(ErrMsg.ID.TAGDIFF_RESULTS_GAP, Path.GetFileName(_arg_src2), Path.GetFileName(_arg_src1)),
                        true, WriteOnce.ConsoleVerbosity.High);
                equal2 = CompareTags(file2Tags.Tags, file1Tags.Tags);

                //final results
                bool resultsDiffer = !(equal1 && equal2);
                if (_arg_tagTestType == TagTestType.Inequality && resultsDiffer)
                    successResult = true;
                else if (_arg_tagTestType == TagTestType.Equality && !resultsDiffer)
                    successResult = true;
                else
                    successResult = false;

                WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.TAGDIFF_RESULTS_DIFFER), false);
                WriteOnce.Result(resultsDiffer.ToString());
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
            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "Tagdiff"));

            return successResult ? (int)ExitCode.TestPassed : (int)ExitCode.TestFailed;
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
                    WriteOnce.Result(s1, true, WriteOnce.ConsoleVerbosity.High);
                }
            }

            //none missing
            if (found)
                WriteOnce.Result(ErrMsg.GetString(ErrMsg.ID.TAGTEST_RESULTS_NONE), true, WriteOnce.ConsoleVerbosity.High);

            return found;
        }


    }
}
