// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.ApplicationInspector.Commands;
using System;
using System.IO;

namespace Microsoft.ApplicationInspector.CLI
{
    /// <summary>
    /// Wrapper for CLI only output arg validation which may be unique to a command
    /// and for allocating the correct writter type and format writter object
    /// </summary>
    public static class ResultsWriter
    {
        public static void Write(Result result, CLICommandOptions options)
        {
            CommandResultsWriter? writer = WriterFactory.GetWriter(options);
            string commandCompletedMsg;

            //perform type checking and assign final msg string
            if (result is TagTestResult)
            {
                commandCompletedMsg = "Tag Test";
            }
            else if (result is TagDiffResult)
            {
                commandCompletedMsg = "Tag Diff";
            }
            else if (result is ExportTagsResult)
            {
                commandCompletedMsg = "Export Tags";
            }
            else if (result is VerifyRulesResult)
            {
                commandCompletedMsg = "Verify Rules";
            }
            else if (result is PackRulesResult)
            {
                commandCompletedMsg = "Pack Rules";
            }
            else if (result is AnalyzeResult analyzeResult && options is CLIAnalyzeCmdOptions cLIAnalyzeCmdOptions) //special handling for html format
            {
                commandCompletedMsg = "Analyze";

                //additional prechecks required for analyze html format
                if (cLIAnalyzeCmdOptions.OutputFileFormat == "html")
                {
                    int MAX_HTML_REPORT_FILE_SIZE = 1024 * 1000 * 3;  //warn about potential slow rendering

                    //prechecks
                    if (analyzeResult.ResultCode != AnalyzeResult.ExitCode.Success)
                    {
                        Finalize(writer, commandCompletedMsg);
                        return;
                    }

                    writer?.WriteResults(analyzeResult, cLIAnalyzeCmdOptions);

                    //post checks
                    if (File.Exists(options.OutputFilePath) && new FileInfo(options.OutputFilePath).Length > MAX_HTML_REPORT_FILE_SIZE)
                    {
                        WriteOnce.Info(MsgHelp.GetString(MsgHelp.ID.ANALYZE_REPORTSIZE_WARN));
                    }

                    if (!cLIAnalyzeCmdOptions.SuppressBrowserOpen)
                    {
                        Utils.OpenBrowser(cLIAnalyzeCmdOptions.OutputFilePath);
                    }

                    Finalize(writer, "Analyze");
                    return;
                }
            }
            else
            {
                throw new Exception("Unrecognized object types for write results");
            }

            //general for all but analyze html format
            writer?.WriteResults(result, options);
            Finalize(writer, commandCompletedMsg);
        }

        /// <summary>
        /// Allow for final actions if even and common file path notice to console
        /// Most Writer.Write operations flushandclose the stream automatically but .Flush
        /// </summary>
        /// <param name="_outputWriter"></param>
        /// <param name="options"></param>
        public static void Finalize(CommandResultsWriter? outputWriter, string commandName)
        {
            WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_COMPLETED, commandName));

            if (outputWriter != null && outputWriter.TextWriter != null)
            {
                if (outputWriter.TextWriter != Console.Out) //target writer was to a file so inform where to find results
                {
                    WriteOnce.Info(MsgHelp.FormatString(MsgHelp.ID.CMD_VIEW_OUTPUT_FILE, outputWriter?.OutputFileName??""), true, WriteOnce.ConsoleVerbosity.Medium, false);
                }
            }
        }
    }
}