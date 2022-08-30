// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.ApplicationInspector.CLI.Writers;
using Microsoft.ApplicationInspector.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.CLI;

public class WriterFactory
{
    private readonly ILogger<WriterFactory> _logger;
    private readonly ILoggerFactory? _loggerFactory;

    public WriterFactory(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory?.CreateLogger<WriterFactory>() ?? NullLogger<WriterFactory>.Instance;
    }

    /// <summary>
    ///     Responsible for returning the correct cmd and format writer for output of cmd results.  An an output
    ///     file will be opened as a stream if provided otherwise the console.out stream is used
    ///     A downcast is expected as the input param containing the common output format and filepath for simplifying
    ///     the allocation to a single method and serves as a type selector but is also recast for command specific
    ///     options in the writer as needed
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public CommandResultsWriter GetWriter(CLICommandOptions options)
    {
        return options switch
        {
            CLIAnalyzeCmdOptions cliAnalyzeCmdOptions => GetAnalyzeWriter(cliAnalyzeCmdOptions),
            CLITagDiffCmdOptions cliTagDiffCmdOptions => GetTagDiffWriter(cliTagDiffCmdOptions),
            CLIExportTagsCmdOptions cliExportTagsCmdOptions => GetExportTagsWriter(cliExportTagsCmdOptions),
            CLIVerifyRulesCmdOptions cliVerifyRulesCmdOptions => GetVerifyRulesWriter(cliVerifyRulesCmdOptions),
            CLIPackRulesCmdOptions cliPackRulesCmdOptions => GetPackRulesWriter(cliPackRulesCmdOptions),
            _ => throw new OpException($"Unrecognized object type {options.GetType().Name} in writer request")
        };
    }

    /// <summary>
    ///     Only AnalyzeResultsWriter supports an html option
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    private CommandResultsWriter GetAnalyzeWriter(CLIAnalyzeCmdOptions options)
    {
        var textWriter = GetTextWriter(options.OutputFilePath);
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
        var writer = GetTextWriter(options.OutputFilePath);
        return options.OutputFileFormat.ToLower() switch
        {
            "json" => new JsonWriter(writer, _loggerFactory),
            "text" => new ExportTagsTextWriter(writer, _loggerFactory),
            _ => throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"))
        };
    }

    private CommandResultsWriter GetTagDiffWriter(CLITagDiffCmdOptions options)
    {
        var writer = GetTextWriter(options.OutputFilePath);
        return options.OutputFileFormat.ToLower() switch
        {
            "json" => new JsonWriter(writer, _loggerFactory),
            "text" => new TagDiffTextWriter(writer, _loggerFactory),
            _ => throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"))
        };
    }

    private CommandResultsWriter GetVerifyRulesWriter(CLIVerifyRulesCmdOptions options)
    {
        var writer = GetTextWriter(options.OutputFilePath);
        return options.OutputFileFormat.ToLower() switch
        {
            "json" => new JsonWriter(writer, _loggerFactory),
            "text" => new VerifyRulesTextWriter(writer, _loggerFactory),
            _ => throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"))
        };
    }

    private CommandResultsWriter GetPackRulesWriter(CLIPackRulesCmdOptions options)
    {
        var writer = GetTextWriter(options.OutputFilePath);
        return options.OutputFileFormat.ToLower() switch
        {
            "json" => new JsonWriter(writer, _loggerFactory),
            _ => throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"))
        };
    }

    /// <summary>
    ///     Create a TextWriter for the given path or console.
    /// </summary>
    /// <param name="outputFileName">The path to create, if null or empty will use Console.Out.</param>
    /// <returns></returns>
    private TextWriter GetTextWriter(string? outputFileName)
    {
        TextWriter textWriter;
        if (string.IsNullOrEmpty(outputFileName))
            textWriter = Console.Out;
        else
            try
            {
                textWriter = File.CreateText(outputFileName);
            }
            catch (Exception)
            {
                _logger.LogError(MsgHelp.GetString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR), outputFileName);
                throw;
            }

        return textWriter;
    }
}