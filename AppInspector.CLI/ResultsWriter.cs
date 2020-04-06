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
        public static void Write(TagDiffResult tagDiffResult, CLITagDiffCmdOptions options)
        {
            CommandResultsWriter writer = WriterFactory.GetWriter(options);
            writer.WriteResults(tagDiffResult, options);
            Finalize(writer, "Tag Diff");
        }

        public static void Write(TagTestResult tagTestResult, CLITagTestCmdOptions options)
        {
            CommandResultsWriter writer = WriterFactory.GetWriter(options);
            writer.WriteResults(tagTestResult, options);
            Finalize(writer, "Tag Test");
        }

        public static void Write(ExportTagsResult exportTagResult, CLIExportTagsCmdOptions options)
        {
            CommandResultsWriter writer = WriterFactory.GetWriter(options);
            writer.WriteResults(exportTagResult, options);
            Finalize(writer, "Export Tags");
        }

        public static void Write(VerifyRulesResult verifyRulesResult, CLIVerifyRulesCmdOptions options)
        {
            CommandResultsWriter writer = WriterFactory.GetWriter(options);
            writer.WriteResults(verifyRulesResult, options);
            Finalize(writer, "Verify Rules");
        }

        public static void Write(PackRulesResult packRulesResult, CLIPackRulesCmdOptions options)
        {
            CommandResultsWriter writer = WriterFactory.GetWriter(options);
            writer.WriteResults(packRulesResult, options);
            Finalize(writer, "Pack Rules");
        }


        /// <summary>
        /// The only Write method that doesn't take a result object due to HTML needs which require more than 
        /// </summary>
        /// <param name="appProfile"></param>
        /// <param name="options"></param>
        public static void Write(AnalyzeResult analyzeResult, CLIAnalyzeCmdOptions options)
        {
            int MAX_HTML_REPORT_FILE_SIZE = 1024 * 1000 * 3;  //warn about potential slow rendering

            //analyze with html format limit checks
            if (options.OutputFileFormat == "html")
            {
                options.OutputFilePath = "output.html";

                AnalyzeHtmlWriter analyzeHtmlWriter = (AnalyzeHtmlWriter)WriterFactory.GetWriter(options);
                analyzeHtmlWriter.WriteResults(analyzeResult, options);

                //html report size warning
                if (File.Exists(options.OutputFilePath) && new FileInfo(options.OutputFilePath).Length > MAX_HTML_REPORT_FILE_SIZE)
                {
                    WriteOnce.Info(MsgHelp.GetString(MsgHelp.ID.ANALYZE_REPORTSIZE_WARN));
                }

                WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_COMPLETED, "Analyze"));

                if (options.OutputFileFormat == "html" && !options.SuppressBrowserOpen)
                {
                    Utils.OpenBrowser(options.OutputFilePath);
                }
            }
            else
            {
                CommandResultsWriter writer = WriterFactory.GetWriter(options);
                writer.WriteResults(analyzeResult, options);
                Finalize(writer, "Analyze");
            }

        }


        /// <summary>
        /// Allow for final actions if even and common file path notice to console
        /// Most Writer.Write operations flushandclose the stream automatically but .Flush
        /// </summary>
        /// <param name="_outputWriter"></param>
        /// <param name="options"></param>
        static public void Finalize(CommandResultsWriter outputWriter, string commandName)
        {
            WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_COMPLETED, commandName));

            if (outputWriter != null && outputWriter.TextWriter != null)
            {
                if (outputWriter.TextWriter != Console.Out) //target writer was to a file so inform where to find results
                {
                    WriteOnce.Info(MsgHelp.FormatString(MsgHelp.ID.CMD_VIEW_OUTPUT_FILE, outputWriter.OutputFileName), true, WriteOnce.ConsoleVerbosity.Medium, false);
                }
            }
        }

    }
}
