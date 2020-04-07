// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.CLI.Writers;
using Microsoft.ApplicationInspector.Commands;

using System;
using System.IO;

namespace Microsoft.ApplicationInspector.CLI
{
    public class WriterFactory
    {
        /// <summary>
        /// Responsible for returning the correct cmd and format writer for output of cmd results.  An an output
        /// file will be opened as a stream if provided otherwise the console.out stream is used
        /// A downcast is expected as the input param containing the common output format and filepath for simplifying
        /// the allocation to a single method and serves as a type selector but is also recast for command specific 
        /// options in the writer as needed 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static CommandResultsWriter GetWriter(CLICommandOptions options)
        {
            CommandResultsWriter writer = null;

            //allocate the right writer by cmd (options) type
            if (options is CLIAnalyzeCmdOptions)
            {
                writer = GetAnalyzeWriter(options);
            }
            else if (options is CLITagTestCmdOptions)
            {
                writer = GetTagTestWriter(options);
            }
            else if (options is CLITagDiffCmdOptions)
            {
                writer = GetTagDiffWriter(options);
            }
            else if (options is CLIExportTagsCmdOptions)
            {
                writer = GetExportWriter(options);
            }
            else if (options is CLIVerifyRulesCmdOptions)
            {
                writer = GetVerifyRulesWriter(options);
            }
            else if (options is CLIPackRulesCmdOptions)
            {
                writer = GetPackRulesWriter(options);
            }

            return writer;
        }


        /// <summary>
        /// Only AnalyzeResultsWriter supports an html option
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static CommandResultsWriter GetAnalyzeWriter(CLICommandOptions options)
        {
            CLIAnalyzeCmdOptions cLIAnalyzeCmdOptions = (CLIAnalyzeCmdOptions)options;
            CommandResultsWriter writer = null;

            switch (cLIAnalyzeCmdOptions.OutputFileFormat.ToLower())
            {
                case "_dummy":
                    writer = new AnalyzeDummyWriter();
                    break;
                case "json":
                    writer = new AnalyzeJsonWriter();
                    break;
                case "text":
                    writer = new AnalyzeTextWriter(cLIAnalyzeCmdOptions.TextOutputFormat);
                    break;
                case "html":
                    writer = new AnalyzeHtmlWriter();
                    break;
                default:
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"));
                    throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")));
            }

            //assign the stream as a file or console
            writer.OutputFileName = cLIAnalyzeCmdOptions.OutputFilePath;
            writer.TextWriter = GetTextWriter(writer.OutputFileName);

            return writer;
        }


        public static CommandResultsWriter GetExportWriter(CLICommandOptions options)
        {
            CLIExportTagsCmdOptions cLIAnalyzeCmdOptions = (CLIExportTagsCmdOptions)options;
            CommandResultsWriter writer = null;

            switch (cLIAnalyzeCmdOptions.OutputFileFormat.ToLower())
            {
                case "_dummy":
                    writer = new ExportDummyWriter();
                    break;
                case "json":
                    writer = new JsonWriter();
                    break;
                case "text":
                    writer = new ExportTextWriter();
                    break;
                default:
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"));
                    throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")));
            }

            //assign the stream as a file or console
            writer.OutputFileName = cLIAnalyzeCmdOptions.OutputFilePath;
            writer.TextWriter = GetTextWriter(writer.OutputFileName);

            return writer;

        }

        private static CommandResultsWriter GetTagTestWriter(CLICommandOptions options)
        {
            CLITagTestCmdOptions cLIAnalyzeCmdOptions = (CLITagTestCmdOptions)options;
            CommandResultsWriter writer = null;

            switch (cLIAnalyzeCmdOptions.OutputFileFormat.ToLower())
            {
                case "_dummy":
                    writer = new TagTestDummyWriter();
                    break;
                case "json":
                    writer = new JsonWriter();
                    break;
                case "text":
                    writer = new TagTestTextWriter();
                    break;
                default:
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"));
                    throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")));
            }

            //assign the stream as a file or console
            writer.OutputFileName = cLIAnalyzeCmdOptions.OutputFilePath;
            writer.TextWriter = GetTextWriter(writer.OutputFileName);

            return writer;
        }

        private static CommandResultsWriter GetTagDiffWriter(CLICommandOptions options)
        {
            CLITagDiffCmdOptions cLIAnalyzeCmdOptions = (CLITagDiffCmdOptions)options;
            CommandResultsWriter writer = null;

            switch (cLIAnalyzeCmdOptions.OutputFileFormat.ToLower())
            {
                case "_dummy":
                    writer = new TagDiffDummyWriter();
                    break;
                case "json":
                    writer = new JsonWriter();
                    break;
                case "text":
                    writer = new TagDiffTextWriter();
                    break;
                default:
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"));
                    throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")));
            }

            //assign the stream as a file or console
            writer.OutputFileName = cLIAnalyzeCmdOptions.OutputFilePath;
            writer.TextWriter = GetTextWriter(writer.OutputFileName);

            return writer;
        }

        private static CommandResultsWriter GetVerifyRulesWriter(CLICommandOptions options)
        {
            CLIVerifyRulesCmdOptions cLIAnalyzeCmdOptions = (CLIVerifyRulesCmdOptions)options;
            CommandResultsWriter writer = null;

            switch (cLIAnalyzeCmdOptions.OutputFileFormat.ToLower())
            {
                case "_dummy":
                    writer = new VerifyRulesDummyWriter();
                    break;
                case "json":
                    writer = new JsonWriter();
                    break;
                case "text":
                    writer = new VerifyRulesTextWriter();
                    break;
                default:
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"));
                    throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")));
            }

            //assign the stream as a file or console
            writer.OutputFileName = cLIAnalyzeCmdOptions.OutputFilePath;
            writer.TextWriter = GetTextWriter(writer.OutputFileName);

            return writer;
        }

        private static CommandResultsWriter GetPackRulesWriter(CLICommandOptions options)
        {
            CLIPackRulesCmdOptions cLIAnalyzeCmdOptions = (CLIPackRulesCmdOptions)options;
            CommandResultsWriter writer = null;

            switch (cLIAnalyzeCmdOptions.OutputFileFormat.ToLower())
            {
                case "_dummy":
                    writer = new PackRulesDummyWriter();
                    break;
                case "json":
                    writer = new JsonWriter();
                    break;
                default:
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"));
                    throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")));
            }

            //assign the stream as a file or console
            writer.OutputFileName = cLIAnalyzeCmdOptions.OutputFilePath;
            writer.TextWriter = GetTextWriter(writer.OutputFileName);
            return writer;
        }

        private static TextWriter GetTextWriter(string outputFileName)
        {
            TextWriter textWriter;
            if (String.IsNullOrEmpty(outputFileName))
            {
                textWriter = Console.Out;
            }
            else
            {
                try
                {
                    textWriter = File.CreateText(outputFileName);
                }
                catch (Exception e)
                {
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, outputFileName));
                    throw new OpException(e.Message);
                }
            }

            return textWriter;

        }

    }
}
