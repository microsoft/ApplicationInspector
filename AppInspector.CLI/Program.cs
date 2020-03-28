//Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using CommandLine;
using Microsoft.ApplicationInspector.Commands;
using NLog;
using System;



namespace Microsoft.ApplicationInspector.CLI
{
    class Program
    {
        /// <summary>
        /// CLI program entry point which defines command verbs and options to running
        /// </summary>
        /// <param name="args"></param>
        static int Main(string[] args)
        {
            int finalResult = (int)Utils.ExitCode.CriticalError;

            Utils.CLIExecutionContext = true;//set manually at start from CLI 

            WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.Medium;
            try
            {
                var argsResult = Parser.Default.ParseArguments<AnalyzeCommandOptions,
                    TagDiffCommandOptions,
                    TagTestCommandOptions,
                    ExportTagsCommandOptions,
                    VerifyRulesCommandOptions,
                    PackRulesCommandOptions>(args)
                  .MapResult(
                    (AnalyzeCommandOptions opts) => RunAnalyzeCommand(opts),
                    (TagDiffCommandOptions opts) => RunTagDiffCommand(opts),
                    (TagTestCommandOptions opts) => RunTagTestCommand(opts),
                    (ExportTagsCommandOptions opts) => RunExportTagsCommand(opts),
                    (VerifyRulesCommandOptions opts) => RunVerifyRulesCommand(opts),
                    (PackRulesCommandOptions opts) => RunPackRulesCommand(opts),
                    errs => 1
                  );

                finalResult = argsResult;

            }
            catch (Exception) //a controlled exit; details not req but written out in command to ensure NuGet+CLI both can console write and log the error
            {
                if (!String.IsNullOrEmpty(Utils.LogFilePath))
                    WriteOnce.Info(ErrMsg.FormatString(ErrMsg.ID.RUNTIME_ERROR_NAMED, Utils.LogFilePath), true, WriteOnce.ConsoleVerbosity.Low, false);
                else
                    WriteOnce.Info(ErrMsg.GetString(ErrMsg.ID.RUNTIME_ERROR_PRELOG), true, WriteOnce.ConsoleVerbosity.Medium, false);

                return finalResult;//avoid double reporting
            }

            if (finalResult == (int)Utils.ExitCode.CriticalError) //case where exception not thrown but result was still a failure; Run() vs constructor exception etc.
            {
                if (!String.IsNullOrEmpty(Utils.LogFilePath))
                    WriteOnce.Info(ErrMsg.FormatString(ErrMsg.ID.RUNTIME_ERROR_UNNAMED, Utils.LogFilePath), true, WriteOnce.ConsoleVerbosity.Low, false);
                else
                    WriteOnce.Info(ErrMsg.GetString(ErrMsg.ID.RUNTIME_ERROR_PRELOG), true, WriteOnce.ConsoleVerbosity.Medium, false);
            }


            return finalResult;
        }


        private static int RunAnalyzeCommand(AnalyzeCommandOptions opts)
        {
            Logger logger = Utils.SetupLogging(opts);
            WriteOnce.Log = logger;
            opts.Log = logger;

            return new AnalyzeCommand(opts).Run();
        }

        private static int RunTagDiffCommand(TagDiffCommandOptions opts)
        {
            Logger logger = Utils.SetupLogging(opts);
            WriteOnce.Log = logger;
            opts.Log = logger;

            return new TagDiffCommand(opts).Run();
        }

        private static int RunTagTestCommand(TagTestCommandOptions opts)
        {
            Logger logger = Utils.SetupLogging(opts);
            WriteOnce.Log = logger;
            opts.Log = logger;

            return new TagTestCommand(opts).Run();
        }

        private static int RunExportTagsCommand(ExportTagsCommandOptions opts)
        {
            Logger logger = Utils.SetupLogging(opts);
            WriteOnce.Log = logger;
            opts.Log = logger;

            return new ExportTagsCommand(opts).Run();
        }

        private static int RunVerifyRulesCommand(VerifyRulesCommandOptions opts)
        {
            Logger logger = Utils.SetupLogging(opts);
            WriteOnce.Log = logger;
            opts.Log = logger;

            return new VerifyRulesCommand(opts).Run();
        }


        private static int RunPackRulesCommand(PackRulesCommandOptions opts)
        {
            Logger logger = Utils.SetupLogging(opts);
            WriteOnce.Log = logger;
            opts.Log = logger;

            return new PackRulesCommand(opts).Run();
        }


    }
}
