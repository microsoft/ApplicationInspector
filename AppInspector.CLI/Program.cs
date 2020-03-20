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

        static public Logger Logger { get; set; }

        /// <summary>
        /// CLI program entry point which defines command verbs and options to running
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
            catch (OpException e)
            {
                if (Logger != null)
                {
                    WriteOnce.Error(ErrMsg.FormatString(ErrMsg.ID.RUNTIME_ERROR_NAMED, e.Message, Utils.LogFilePath));
                    Logger.Error($"Runtime error: {e.StackTrace}");
                }
                else
                    WriteOnce.Error(ErrMsg.FormatString(ErrMsg.ID.RUNTIME_ERROR_PRELOG, e.Message));
            }
            catch (Exception e)
            {
                if (Logger != null)
                {
                    WriteOnce.Error(ErrMsg.FormatString(ErrMsg.ID.RUNTIME_ERROR_UNNAMED, Utils.LogFilePath));
                    Logger.Error($"Runtime error: {e.Message} {e.StackTrace}");
                }
                else
                    WriteOnce.Error(ErrMsg.FormatString(ErrMsg.ID.RUNTIME_ERROR_PRELOG, e.Message));
            }

            return finalResult;
        }


        private static int RunAnalyzeCommand(AnalyzeCommandOptions opts)
        {
            Logger = Utils.SetupLogging(opts);
            WriteOnce.Log = Logger;
            opts.Log = Logger;

            return new AnalyzeCommand(opts).Run();
        }

        private static int RunTagDiffCommand(TagDiffCommandOptions opts)
        {
            Logger = Utils.SetupLogging(opts);
            WriteOnce.Log = Logger;
            opts.Log = Logger;

            return new TagDiffCommand(opts).Run();
        }

        private static int RunTagTestCommand(TagTestCommandOptions opts)
        {
            Logger = Utils.SetupLogging(opts);
            WriteOnce.Log = Logger;
            opts.Log = Logger;

            return new TagTestCommand(opts).Run();
        }

        private static int RunExportTagsCommand(ExportTagsCommandOptions opts)
        {
            Logger = Utils.SetupLogging(opts);
            WriteOnce.Log = Logger;
            opts.Log = Logger;

            return new ExportTagsCommand(opts).Run();
        }

        private static int RunVerifyRulesCommand(VerifyRulesCommandOptions opts)
        {
            Logger = Utils.SetupLogging(opts);
            WriteOnce.Log = Logger;
            opts.Log = Logger;

            return new VerifyRulesCommand(opts).Run();
        }


        private static int RunPackRulesCommand(PackRulesCommandOptions opts)
        {
            Logger = Utils.SetupLogging(opts);
            WriteOnce.Log = Logger;
            opts.Log = Logger;

            return new PackRulesCommand(opts).Run();
        }


    }
}
