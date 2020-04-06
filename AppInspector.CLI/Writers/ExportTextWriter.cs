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

            //For text output, update write once for same results to console or file
            WriteOnce.TextWriter = TextWriter;
            WriteOnce.Result("Result details:");

            if (exportTagsResult.TagsList.Count > 0)
                WriteOnce.General("Tags");

            foreach (string tag in exportTagsResult.TagsList)
                WriteOnce.General(tag);

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