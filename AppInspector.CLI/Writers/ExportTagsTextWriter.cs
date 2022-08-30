// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.CLI;

internal class ExportTagsTextWriter : CommandResultsWriter
{
    private readonly ILogger<ExportTagsTextWriter> _logger;

    internal ExportTagsTextWriter(TextWriter textWriter, ILoggerFactory? loggerFactory = null) : base(textWriter)
    {
        _logger = loggerFactory?.CreateLogger<ExportTagsTextWriter>() ?? NullLogger<ExportTagsTextWriter>.Instance;
    }

    public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
    {
        if (TextWriter is null) throw new ArgumentNullException(nameof(TextWriter));

        var exportTagsResult = (ExportTagsResult)result;

        if (exportTagsResult.TagsList.Count > 0)
        {
            TextWriter.WriteLine("Results");

            foreach (var tag in exportTagsResult.TagsList) TextWriter.WriteLine(tag);
        }
        else
        {
            TextWriter.WriteLine("No tags found");
        }

        if (autoClose) FlushAndClose();
    }
}