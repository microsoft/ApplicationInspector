//Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommandLine;
using System.Reflection;
using NLog;
using NLog.Targets;
using NLog.Config;
using System.IO;


namespace Microsoft.AppInspector
{
    #region CommandLineArgOptions

    /// <summary>
    /// Command option classes for each command verb
    /// </summary>

    [Verb("analyze", HelpText = "Inspect source directory/file/compressed file (.tgz|zip) against defined characteristics")]
    public class AnalyzeCommandOptions
    {
        [Option('s', "source-path", Required = true, HelpText = "Path to source code to inspect (required)")]
        public string SourcePath { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('f', "output-file-format", Required = false, HelpText = "Output format [html|json|text]", Default = "html")]
        public string OutputFileFormat { get; set; }

        [Option('e', "text-format", Required = false, HelpText = "Match text format specifiers", Default = "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Sourcetype:%t,Line:%L,Sample:%m")]
        public string TextOutputFormat { get; set; }

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('t', "tag-output-only", Required = false, HelpText = "Output only identified tags", Default = false)]
        public bool SimpleTagsOnly { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('d', "allow-dup-tags", Required = false, HelpText = "Output contains unique and non-unique tag matches", Default = false)]
        public bool AllowDupTags { get; set; }

        [Option('b', "supress-browser-open", Required = false, HelpText = "HTML formatted output is automatically opened to default browser", Default = false)]
        public bool AutoBrowserOpen { get; set; }

        [Option('c', "confidence-filters", Required = false, HelpText = "Output only matches with specified confidence <value>,<value> [high|medium|low]", Default = "high,medium")]
        public string ConfidenceFilters { get; set; }

        [Option('k', "file-path-exclusions", Required = false, HelpText = "Exclude source files (none|default: sample,example,test,docs,.vs,.git)", Default = "sample,example,test,docs,.vs,.git")]
        public string FilePathExclusions { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }

        #region logoptions

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level [Debug|Info|Warn|Error|Fatal|Off]", Default = "Error")]
        public string LogFileLevel { get; set; }

        #endregion
    }

    [Verb("tagdiff", HelpText = "Compares unique tag values between two source paths")]
    public class TagDiffCommandOptions
    {
        [Option("src1", Required = true, HelpText = "Source 1 to compare (required)")]
        public string SourcePath1 { get; set; }

        [Option("src2", Required = true, HelpText = "Source 2 to compare (required")]
        public string SourcePath2 { get; set; }

        [Option('t', "test-type", Required = false, HelpText = "Type of test to run [equality|inequality]", Default = "equality")]
        public string TestType { get; set; }

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }


        #region logoptions

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level [Debug|Info|Warn|Error|Fatal|Off]", Default = "Error")]
        public string LogFileLevel { get; set; }


        #endregion
    }

    [Verb("tagtest", HelpText = "Test presence of smaller set or custom tags in source (compare or verify modes)")]
    public class TagTestCommandOptions
    {
        [Option('s', "source-path", Required = true, HelpText = "Source to test (required)")]
        public string SourcePath { get; set; }

        [Option('t', "test-type", Required = false, HelpText = "Test to perform [rulespresent|rulesnotpresent] ", Default = "rulespresent")]
        public string TestType { get; set; }

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = true)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }


        #region logoptions

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level [Debug|Info|Warn|Error|Fatal|Off]", Default = "Error")]
        public string LogFileLevel { get; set; }


        #endregion
    }

    [Verb("exporttags", HelpText = "Export default unique rule tags to view what features may be detected")]
    public class ExportTagsCommandOptions
    {
        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }

        #region logoptions

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level [Debug|Info|Warn|Error|Fatal|Off]", Default = "Error")]
        public string LogFileLevel { get; set; }


        #endregion
    }


    [Verb("verifyrules", HelpText = "Verify rules syntax is valid")]
    public class VerifyRulesCommandOptions
    {
        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }

        #region logoptions

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level [Debug|Info|Warn|Error|Fatal|Off]", Default = "Error")]
        public string LogFileLevel { get; set; }


        #endregion
    }

    #endregion


    class Program
    {

        public static string GetVersionString()
        {
            return String.Format("Microsoft Application Inspector {0}", GetVersion());
        }

        public static string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion;
        }

        static public Logger Logger { get; set; }


        /// <summary>
        /// Program entry point which defines command verbs and options to running
        /// </summary>
        /// <param name="args"></param>
        static int Main(string[] args)
        {
            int finalResult = -1;
            
            WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.Medium;

            try
            {
                WriteOnce.Info(GetVersionString());
                var argsResult = Parser.Default.ParseArguments<AnalyzeCommandOptions, 
                    TagDiffCommandOptions,
                    TagTestCommandOptions,
                    ExportTagsCommandOptions,
                    VerifyRulesCommandOptions>(args)
                  .MapResult(
                    (AnalyzeCommandOptions opts) => RunAnalyzeCommand(opts),
                    (TagDiffCommandOptions opts) => RunTagDiffCommand(opts),
                    (TagTestCommandOptions opts) => RunTagTestCommand(opts),
                    (ExportTagsCommandOptions opts) => RunExportTagsCommand(opts),
                    (VerifyRulesCommandOptions opts) => RunVerifyRulesCommand(opts),
                    errs => 1
                  );

                finalResult = argsResult;

            }
            catch (OpException e)
            {
                if (Logger != null)
                {
                    WriteOnce.Error(ErrMsg.FormatString(ErrMsg.ID.RUNTIME_ERROR_NAMED, e.Message));
                    Logger.Error($"Runtime error: {e.StackTrace}");
                }
                else
                    WriteOnce.Error(ErrMsg.FormatString(ErrMsg.ID.RUNTIME_ERROR_PRELOG, e.Message));
            }
            catch (Exception e)
            {
                if (Logger != null)
                {
                    WriteOnce.Error(ErrMsg.FormatString(ErrMsg.ID.RUNTIME_ERROR_UNNAMED));
                    Logger.Error($"Runtime error: {e.Message} {e.StackTrace}");
                }
                else
                    WriteOnce.Error(ErrMsg.FormatString(ErrMsg.ID.RUNTIME_ERROR_PRELOG, e.Message));
            }

            return finalResult;
        }


        private static int RunAnalyzeCommand(AnalyzeCommandOptions opts)
        {
            SetupLogging(opts.LogFilePath, opts.LogFileLevel);
            return new AnalyzeCommand(opts).Run();
        }

        private static int RunTagDiffCommand(TagDiffCommandOptions opts)
        {
            SetupLogging(opts.LogFilePath, opts.LogFileLevel);
            return new TagDiffCommand(opts).Run();
        }
        
        private static int RunTagTestCommand(TagTestCommandOptions opts)
        {
            SetupLogging(opts.LogFilePath, opts.LogFileLevel);
            return new TagTestCommand(opts).Run();
        }

        private static int RunExportTagsCommand(ExportTagsCommandOptions opts)
        {
            SetupLogging(opts.LogFilePath, opts.LogFileLevel);
            return new ExportTagsCommand(opts).Run();
        }

        private static int RunVerifyRulesCommand(VerifyRulesCommandOptions opts)
        {
            SetupLogging(opts.LogFilePath, opts.LogFileLevel);
            return new VerifyRulesCommand(opts).Run();
        }


        static void SetupLogging(string logFilePath, string logFileLevel)
        {
            var config = new NLog.Config.LoggingConfiguration();


            if (String.IsNullOrEmpty(logFilePath))
            {
                logFilePath = Utils.GetPath(Utils.AppPath.defaultLog);
                //if using default app log path clean up previous for convenience in reading
                if (File.Exists(logFilePath))
                    File.Delete(logFilePath);
            }

            LogLevel log_level = LogLevel.Error;//default
            try
            {
                log_level = LogLevel.FromString(logFileLevel);
            }
            catch (Exception)
            {
                throw new OpException(String.Format(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-v")));
            }
            
            using (var fileTarget = new FileTarget()
            {
                Name = "LogFile",
                FileName = logFilePath,
                Layout = @"${date:universalTime=true:format=s} ${threadid} ${level:uppercase=true} - ${message}",
                ForceMutexConcurrentWrites = true

            })
            {
                config.AddTarget(fileTarget);
                config.LoggingRules.Add(new LoggingRule("*", log_level, fileTarget));
            }

            LogManager.Configuration = config;      
            Logger = LogManager.GetCurrentClassLogger();
            Logger.Info("["+ DateTime.Now.ToLocalTime() + "] //////////////////////////////////////////////////////////");
            WriteOnce.Log = Logger;//allows log to be written to as well as console or output file

        }

    }
}
