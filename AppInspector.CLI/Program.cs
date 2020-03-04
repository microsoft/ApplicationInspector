//Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using CommandLine;
using Microsoft.ApplicationInspector.Commands;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;


namespace Microsoft.ApplicationInspector.CLI
{
    class Program
    {

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
                WriteOnce.Info(Utils.GetVersionString());
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
            SetupLogging(opts);
            return new AnalyzeCommand(opts).Run();
        }

        private static int RunTagDiffCommand(TagDiffCommandOptions opts)
        {
            SetupLogging(opts);
            return new TagDiffCommand(opts).Run();
        }

        private static int RunTagTestCommand(TagTestCommandOptions opts)
        {
            SetupLogging(opts);
            return new TagTestCommand(opts).Run();
        }

        private static int RunExportTagsCommand(ExportTagsCommandOptions opts)
        {
            SetupLogging(opts);
            return new ExportTagsCommand(opts).Run();
        }

        private static int RunVerifyRulesCommand(VerifyRulesCommandOptions opts)
        {
            SetupLogging(opts);
            return new VerifyRulesCommand(opts).Run();
        }


        static void SetupLogging(AllCommandOptions opts)
        {
            var config = new NLog.Config.LoggingConfiguration();

            if (String.IsNullOrEmpty(opts.LogFilePath))
            {
                opts.LogFilePath = Utils.GetPath(Utils.AppPath.defaultLog);
                //if using default app log path clean up previous for convenience in reading
                if (File.Exists(opts.LogFilePath))
                    File.Delete(opts.LogFilePath);
            }

            LogLevel log_level = LogLevel.Error;//default
            try
            {
                log_level = LogLevel.FromString(opts.LogFileLevel);
            }
            catch (Exception)
            {
                throw new OpException(String.Format(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-v")));
            }

            using (var fileTarget = new FileTarget()
            {
                Name = "LogFile",
                FileName = opts.LogFilePath,
                Layout = @"${date:universalTime=true:format=s} ${threadid} ${level:uppercase=true} - ${message}",
                ForceMutexConcurrentWrites = true

            })
            {
                config.AddTarget(fileTarget);
                config.LoggingRules.Add(new LoggingRule("*", log_level, fileTarget));
            }

            LogManager.Configuration = config;
            opts.Log = LogManager.GetCurrentClassLogger();
            Logger = opts.Log;
            Logger.Info("[" + DateTime.Now.ToLocalTime() + "] //////////////////////////////////////////////////////////");
            WriteOnce.Log = Logger;//allows log to be written to as well as console or output file

        }

    }
}
