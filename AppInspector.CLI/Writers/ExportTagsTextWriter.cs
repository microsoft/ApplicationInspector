// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.CLI
{
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.IO;

    internal class ExportTagsTextWriter : CommandResultsWriter
    {
        private readonly ILogger<ExportTagsTextWriter> _logger;

        internal ExportTagsTextWriter(TextWriter textWriter, ILoggerFactory? loggerFactory = null) : base(textWriter)
        {
            _logger = loggerFactory?.CreateLogger<ExportTagsTextWriter>() ?? NullLogger<ExportTagsTextWriter>.Instance;
        }

        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            if (TextWriter is null)
            {
                throw new ArgumentNullException(nameof(TextWriter));
            }

            ExportTagsResult exportTagsResult = (ExportTagsResult)result;

            if (exportTagsResult.TagsList.Count > 0)
            {
                TextWriter.WriteLine("Results");

                foreach (string tag in exportTagsResult.TagsList)
                {
                    TextWriter.WriteLine(tag);
                }
            }
            else
            {
                TextWriter.WriteLine("No tags found");
            }

            if (autoClose)
            {
                FlushAndClose();
            }
        }
    }
}