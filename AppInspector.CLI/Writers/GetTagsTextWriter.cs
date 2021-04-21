// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.Commands;
using System;
using System.Collections.Generic;

namespace Microsoft.ApplicationInspector.CLI
{
    public class GetTagsTextWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            GetTagsResult getTagsResult = (GetTagsResult)result;
            CLIGetTagsCommandOptions cliGetTagsCommandOptions = (CLIGetTagsCommandOptions)commandOptions;

            //For console output, update write once for same results to console or file
            WriteOnce.TextWriter = TextWriter;

            if (string.IsNullOrEmpty(commandOptions.OutputFilePath))
            {
                WriteOnce.Result("Results");
            }

            foreach(var tag in getTagsResult.Metadata.UniqueTags ?? new List<string>())
            {
                WriteOnce.Result(tag);
            }

            if (autoClose)
            {
                FlushAndClose();
            }
        }
    }
}