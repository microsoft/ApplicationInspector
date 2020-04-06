// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.Commands;
using System;

namespace Microsoft.ApplicationInspector.CLI
{
    public class TagDiffTextWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            TagDiffResult tagDiffResult = (TagDiffResult)result;
            CLITagDiffCmdOptions cLITagDiffCmdOptions = (CLITagDiffCmdOptions)commandOptions;

            //For text output, update write once for same results to console or file
            WriteOnce.TextWriter = TextWriter;
            WriteOnce.Result("Result status:");
            WriteOnce.General(MsgHelp.FormatString(MsgHelp.ID.TAGTEST_RESULTS_TEST_TYPE, cLITagDiffCmdOptions.TestType), false, WriteOnce.ConsoleVerbosity.Low);

            if (tagDiffResult.ResultCode == TagDiffResult.ExitCode.TestFailed)
                WriteOnce.Any(MsgHelp.GetString(MsgHelp.ID.TAGTEST_RESULTS_FAIL), true, ConsoleColor.Red, WriteOnce.ConsoleVerbosity.Low);
            else
                WriteOnce.Any(MsgHelp.GetString(MsgHelp.ID.TAGTEST_RESULTS_SUCCESS), true, ConsoleColor.Green, WriteOnce.ConsoleVerbosity.Low);

            //Results list
            if (tagDiffResult.TagDiffList.Count > 0)
                WriteOnce.Result("Result details:");

            foreach (TagDiff tagDiff in tagDiffResult.TagDiffList)
                WriteOnce.General(String.Format("Tag: {0}, Only found in file: {1}", tagDiff.Tag, tagDiff.Source));

            if (autoClose)
                FlushAndClose();
        }

        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
            WriteOnce.TextWriter = null;
        }

    }
}