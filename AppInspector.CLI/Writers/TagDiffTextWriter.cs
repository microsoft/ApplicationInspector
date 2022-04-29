// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.CLI
{
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using System.IO;

    public class TagDiffTextWriter : CommandResultsWriter
    {
        private readonly ILogger<TagDiffTextWriter> _logger;

        public TagDiffTextWriter(TextWriter textWriter, ILoggerFactory? loggerFactory = null) : base(textWriter)
        {
            _logger = loggerFactory?.CreateLogger<TagDiffTextWriter>() ?? NullLogger<TagDiffTextWriter>.Instance;
        }
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            TagDiffResult tagDiffResult = (TagDiffResult)result;
            CLITagDiffCmdOptions cLITagDiffCmdOptions = (CLITagDiffCmdOptions)commandOptions;

            TextWriter.WriteLine(MsgHelp.FormatString(MsgHelp.ID.TAGTEST_RESULTS_TEST_TYPE, cLITagDiffCmdOptions.TestType));

            if (tagDiffResult.ResultCode == TagDiffResult.ExitCode.TestFailed)
            {
                TextWriter.WriteLine(MsgHelp.GetString(MsgHelp.ID.TAGTEST_RESULTS_FAIL));
            }
            else
            {
                TextWriter.WriteLine(MsgHelp.GetString(MsgHelp.ID.TAGTEST_RESULTS_SUCCESS));
            }

            //Results list
            if (tagDiffResult.TagDiffList.Count > 0)
            {
                TextWriter.WriteLine("Differences");
                foreach (TagDiff tagDiff in tagDiffResult.TagDiffList)
                {
                    TextWriter.WriteLine(string.Format("Tag: {0}, Only found in file: {1}", tagDiff.Tag, tagDiff.Source));
                }
            }

            if (autoClose)
            {
                FlushAndClose();
            }
        }
    }
}