// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.Commands;

namespace Microsoft.ApplicationInspector.CLI
{
    internal class ExportTextWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            ExportTagsResult exportTagsResult = (ExportTagsResult)result;

            //For console output, update write once for same results to console or file
            WriteOnce.TextWriter = TextWriter;

            if (exportTagsResult.TagsList.Count > 0)
            {
                WriteOnce.Result("Results");

                foreach (string tag in exportTagsResult.TagsList)
                {
                    WriteOnce.General(tag);
                }
            }
            else
            {
                WriteOnce.General("No tags found");
            }

            if (autoClose)
            {
                FlushAndClose();
            }
        }
    }
}