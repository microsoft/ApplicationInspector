// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.ApplicationInspector.CLI
{
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;

    /// <summary>
    /// Wrapper for CLI only output arg validation which may be unique to a command
    /// and for allocating the correct writter type and format writter object
    /// </summary>
    public class ResultsWriter
    {
        public ResultsWriter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ResultsWriter>();
            _loggerFactory = loggerFactory;
        }

        private ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public void Write(Result result, CLICommandOptions options)
        {
            WriterFactory writerFactory = new WriterFactory(_loggerFactory);
            CommandResultsWriter? writer = writerFactory.GetWriter(options);
            string commandCompletedMsg;

            //perform type checking and assign final msg string
            if (result is TagDiffResult)
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
                if (writer is AnalyzeHtmlWriter)
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
                    if (options.OutputFilePath is not null && File.Exists(options.OutputFilePath) && new FileInfo(options.OutputFilePath).Length > MAX_HTML_REPORT_FILE_SIZE)
                    {
                        _logger.LogInformation(MsgHelp.GetString(MsgHelp.ID.ANALYZE_REPORTSIZE_WARN));
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
        internal void Finalize(CommandResultsWriter? outputWriter, string commandName)
        {
            _logger.LogInformation(MsgHelp.FormatString(MsgHelp.ID.CMD_COMPLETED, commandName));
        }
    }
}