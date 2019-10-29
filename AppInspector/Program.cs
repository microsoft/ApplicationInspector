//Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommandLine;
using System.Reflection;
using Microsoft.AppInspector.Commands;
using NLog;
using NLog.Targets;
using NLog.Config;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.AppInspector
{
    #region CommandLineArgOptions

    /// <summary>
    /// Command option classes for each command verb
    /// </summary>

    [Verb("analyze", HelpText = "Inspect source directory/file against defined characteristics")]
    public class AnalyzeCommandOptions
    {
        private bool uniqueTagsOnly;

        [Option('s', "source-path", Required = true, HelpText = "Path to source code to inspect (required)")]
        public string SourcePath { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('f', "output-file-format", Required = false, HelpText = "Output format [html|json|text]", Default = "html")]
        public string OutputFileFormat { get; set; }

        [Option('e', "text-format", Required = false, HelpText = "Text format specifiers", Default = "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Sourcetype:%t,Line:%L,Sample:%m")]
        public string TextOutputFormat { get; set; }

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('t', "tag-output-only", Required = false, HelpText = "Output only contains identified tags", Default = false)]
        public bool SimpleTagsOnly { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Ignore default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('u', "unique-tags-only", Required = false, HelpText = "Output only contains unique tag matches", Default = true)]
        public bool UniqueTagsOnly
        {
            get { return uniqueTagsOnly; }
            set
            {
                uniqueTagsOnly = value;
            }
        }

        [Option('c', "confidence-filters", Required = false, HelpText = "Outout only if matching confidence <value>,<value> [high|medium|low]", Default = "high,medium")]
        public string ConfidenceFilters { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }

        #region logoptions

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level")]
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

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Ignore default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }


        #region logoptions

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level")]
        public string LogFileLevel { get; set; }


        #endregion
    }

    [Verb("tagtest", HelpText = "Test presence of tags in source (compare or verify modes)")]
    public class TagTestCommandOptions
    {
        [Option('s', "source-path", Required = true, HelpText = "Source to test (required)")]
        public string SourcePath { get; set; }

        [Option('t', "test-type", Required = false, HelpText = "Test to perform [rulespresent|rulesnotpresent] ", Default = "rulespresent")]
        public string TestType { get; set; }

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Ignore default rules bundled with application", Default = true)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }


        #region logoptions

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level")]
        public string LogFileLevel { get; set; }


        #endregion
    }

    [Verb("exporttags", HelpText = "Export unique rule tags")]
    public class ExportTagsCommandOptions
    {
        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Ignore default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        #region logoptions

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level")]
        public string LogFileLevel { get; set; }


        #endregion
    }


    [Verb("verifyrules", HelpText = "Verify rules syntax is valid")]
    public class VerifyRulesCommandOptions
    {
        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Ignore default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        #region logoptions

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level")]
        public string LogFileLevel { get; set; }


        #endregion
    }

    #endregion


    class Program
    {
        static bool _uniqueOverRide = true;

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

        static bool ConsoleNoWait { get; set; }


        /// <summary>
        /// Program entry point which defines command verbs and options to running
        /// </summary>
        /// <param name="args"></param>
        static int Main(string[] args)
        {
            int finalResult = -1;
            
            WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.Medium;
            //TODO had to add work around for very strange behavior from cmdlineparser that will not accept a false from
            //the command line; several attempts to force, rename etc. to no avail.  This works for now.
            if (args.Length > 0 && args[0] == "analyze")
            {
                for (int i=0;i<args.Length;i++)
                {
                    if (args[i] == "-u" || args[i] == "--unique-tags-only")
                        _uniqueOverRide = args[i + 1] == "true";
                }
            }

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
                WriteOnce.Error(Helper.FormatResourceString(ResourceMsg.ID.RUNTIME_ERROR_NAMED, e.Message));
#if DEBUG
                WriteOnce.Error($"Runtime error: {e.Message}\n{e.StackTrace}");//save time debugging else keep console clean
#else
                Logger.Error($"Runtime error: {e.StackTrace}");//no sensitive data expected in output
#endif
            }
            catch (Exception e)
            {
                WriteOnce.Error(Helper.FormatResourceString(ResourceMsg.ID.RUNTIME_ERROR_UNNAMED));
#if DEBUG
                WriteOnce.Error($"Runtime error: {e.Message}\n{e.StackTrace}");//save time debugging else keep console clean
#else
                Logger.Error($"Runtime error: {e.StackTrace}");//no sensitive data expected in output
#endif
            }

            return finalResult;
        }


        private static int RunAnalyzeCommand(AnalyzeCommandOptions opts)
        {
            opts.UniqueTagsOnly = _uniqueOverRide;
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
                logFilePath = Helper.GetPath(Helper.AppPath.defaultLog);
                //if using default app log path clean up previous for convenience in reading
                if (File.Exists(logFilePath))
                    File.Delete(logFilePath);
            }

            if (String.IsNullOrEmpty(logFileLevel))
                logFileLevel = "ERROR";

            using (var fileTarget = new FileTarget()
            {
                Name = "LogFile",
                FileName = logFilePath,
                Layout = @"${date:universalTime=true:format=s} ${threadid} ${level:uppercase=true} - ${message}",
                ForceMutexConcurrentWrites = true

            })
            {
                config.AddTarget(fileTarget);
                config.LoggingRules.Add(new LoggingRule("*", LogLevel.FromString(logFileLevel), fileTarget));
            }

            LogManager.Configuration = config;      
            Logger = LogManager.GetCurrentClassLogger();
            Logger.Info("["+ DateTime.Now.ToLocalTime() + "] //////////////////////////////////////////////////////////");
            WriteOnce.Log = Logger;//allows log to be written to as well as console or output file

        }

    }
}