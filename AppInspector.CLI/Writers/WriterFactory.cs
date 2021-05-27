// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.CLI.Writers;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using System;
using System.IO;

namespace Microsoft.ApplicationInspector.CLI
{
    public static class WriterFactory
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
        public static CommandResultsWriter? GetWriter(CLICommandOptions options)
        {
            CommandResultsWriter? writer;

            //allocate the right writer by cmd (options) type
            if (options is CLIAnalyzeCmdOptions cliAnalyzeCmdOptions)
            {
                writer = GetAnalyzeWriter(cliAnalyzeCmdOptions);
            }
            else if (options is CLITagDiffCmdOptions cliTagDiffCmdOptions)
            {
                writer = GetTagDiffWriter(cliTagDiffCmdOptions);
            }
            else if (options is CLIExportTagsCmdOptions cliExportTagsCmdOptions)
            {
                writer = GetExportTagsWriter(cliExportTagsCmdOptions);
            }
            else if (options is CLIVerifyRulesCmdOptions cliVerifyRulesCmdOptions)
            {
                writer = GetVerifyRulesWriter(cliVerifyRulesCmdOptions);
            }
            else if (options is CLIPackRulesCmdOptions cliPackRulesCmdOptions)
            {
                writer = GetPackRulesWriter(cliPackRulesCmdOptions);
            }
            else
            {
                throw new Exception("Unrecognized object type in writer request");
            }

            return writer;
        }

        /// <summary>
        /// Only AnalyzeResultsWriter supports an html option
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static CommandResultsWriter GetAnalyzeWriter(CLIAnalyzeCmdOptions options)
        {
            CommandResultsWriter? writer;

            switch (options.OutputFileFormat.ToLower())
            {
                case "json":
                    writer = new AnalyzeJsonWriter();
                    break;

                case "text":
                    writer = new AnalyzeTextWriter(options.TextOutputFormat);
                    break;

                case "html":
                    writer = new AnalyzeHtmlWriter();
                    break;

                default:
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"));
                    throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")));
            }

            //assign the stream as a file or console
            writer.OutputFileName = options.OutputFilePath;
            writer.TextWriter = GetTextWriter(writer.OutputFileName);

            return writer;
        }

        public static CommandResultsWriter GetExportTagsWriter(CLIExportTagsCmdOptions options)
        {
            CommandResultsWriter? writer;

            switch (options.OutputFileFormat.ToLower())
            {
                case "json":
                    writer = new JsonWriter();
                    break;

                case "text":
                    writer = new ExportTagsTextWriter();
                    break;

                default:
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"));
                    throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")));
            }

            //assign the stream as a file or console
            writer.OutputFileName = options.OutputFilePath;
            writer.TextWriter = GetTextWriter(writer.OutputFileName);

            return writer;
        }

        private static CommandResultsWriter GetTagDiffWriter(CLITagDiffCmdOptions options)
        {
            CommandResultsWriter? writer;

            switch (options.OutputFileFormat.ToLower())
            {
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
            writer.OutputFileName = options.OutputFilePath;
            writer.TextWriter = GetTextWriter(writer.OutputFileName);

            return writer;
        }

        private static CommandResultsWriter GetVerifyRulesWriter(CLIVerifyRulesCmdOptions options)
        {
            CommandResultsWriter? writer;

            switch (options.OutputFileFormat.ToLower())
            {
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
            writer.OutputFileName = options.OutputFilePath;
            writer.TextWriter = GetTextWriter(writer.OutputFileName);

            return writer;
        }

        private static CommandResultsWriter GetPackRulesWriter(CLIPackRulesCmdOptions options)
        {
            CommandResultsWriter? writer;

            switch (options.OutputFileFormat.ToLower())
            {
                case "json":
                    writer = new JsonWriter();
                    break;

                default:
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"));
                    throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")));
            }

            //assign the stream as a file or console
            writer.OutputFileName = options.OutputFilePath;
            writer.TextWriter = GetTextWriter(writer.OutputFileName);
            return writer;
        }

        private static TextWriter GetTextWriter(string? outputFileName)
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