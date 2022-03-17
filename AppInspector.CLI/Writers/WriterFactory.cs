// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.CLI
{
    using Microsoft.ApplicationInspector.CLI.Writers;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.IO;

    public class WriterFactory
    {
        private readonly ILoggerFactory? _loggerFactory;
        private readonly ILogger<WriterFactory> _logger;

        public WriterFactory(ILoggerFactory? loggerFactory = null)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory?.CreateLogger<WriterFactory>() ?? NullLogger<WriterFactory>.Instance;
        }
        /// <summary>
        /// Responsible for returning the correct cmd and format writer for output of cmd results.  An an output
        /// file will be opened as a stream if provided otherwise the console.out stream is used
        /// A downcast is expected as the input param containing the common output format and filepath for simplifying
        /// the allocation to a single method and serves as a type selector but is also recast for command specific
        /// options in the writer as needed
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public CommandResultsWriter? GetWriter(CLICommandOptions options)
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
        private CommandResultsWriter GetAnalyzeWriter(CLIAnalyzeCmdOptions options)
        {
            TextWriter textWriter = GetTextWriter(options.OutputFilePath);
            return options.OutputFileFormat.ToLower() switch
            {
                "json" => new AnalyzeJsonWriter(textWriter, _loggerFactory),
                "text" => new AnalyzeTextWriter(textWriter, options.TextOutputFormat, _loggerFactory),
                "html" => new AnalyzeHtmlWriter(textWriter, _loggerFactory),
                "sarif" => new AnalyzeSarifWriter(textWriter, _loggerFactory),
                _ => throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"))
            };
        }

        public CommandResultsWriter GetExportTagsWriter(CLIExportTagsCmdOptions options)
        {
            TextWriter writer = GetTextWriter(options.OutputFilePath);
            return options.OutputFileFormat.ToLower() switch
            {
                "json" => new JsonWriter(writer, _loggerFactory),
                "text" => new ExportTagsTextWriter(writer, _loggerFactory),
                _ => throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")))
            };
        }

        private CommandResultsWriter GetTagDiffWriter(CLITagDiffCmdOptions options)
        {
            TextWriter writer = GetTextWriter(options.OutputFilePath);
            return options.OutputFileFormat.ToLower() switch
            {
                "json" => new JsonWriter(writer, _loggerFactory),
                "text" => new TagDiffTextWriter(writer, _loggerFactory),
                _ => throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")))
            };
        }

        private CommandResultsWriter GetVerifyRulesWriter(CLIVerifyRulesCmdOptions options)
        {
            TextWriter writer = GetTextWriter(options.OutputFilePath);
            return options.OutputFileFormat.ToLower() switch
            {
                "json" => new JsonWriter(writer, _loggerFactory),
                "text" => new VerifyRulesTextWriter(writer, _loggerFactory),
                _ => throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")))
            };
        }

        private CommandResultsWriter GetPackRulesWriter(CLIPackRulesCmdOptions options)
        {
            TextWriter writer = GetTextWriter(options.OutputFilePath);
            return options.OutputFileFormat.ToLower() switch
            {
                "json" => new JsonWriter(writer, _loggerFactory),
                _ => throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f")))
            };
        }

        /// <summary>
        /// Create a TextWriter for the given path or console.
        /// </summary>
        /// <param name="outputFileName">The path to create, if null or empty will use Console.Out.</param>
        /// <returns></returns>
        private TextWriter GetTextWriter(string? outputFileName)
        {
            TextWriter textWriter;
            if (string.IsNullOrEmpty(outputFileName))
            {
                textWriter = Console.Out;
            }
            else
            {
                try
                {
                    textWriter = File.CreateText(outputFileName);
                }
                catch (Exception)
                {
                    _logger.LogError(MsgHelp.GetString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR), outputFileName);
                    throw;
                }
            }

            return textWriter;
        }
    }
}