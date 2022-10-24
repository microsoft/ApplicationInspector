// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.CLI;

/// <summary>
///     Writes in json format
///     Users can select arguments to filter output to 1. only simple tags 2. only matchlist without rollup metadata etc.
///     3. everything
///     Lists of tagreportgroups are written as well as match list details so users have chose to present the same
///     UI as shown in the HTML report to the level of detail desired...
/// </summary>
public class AnalyzeJsonWriter : CommandResultsWriter
{
    private readonly ILogger<AnalyzeJsonWriter> _logger;

    public AnalyzeJsonWriter(TextWriter textWriter, ILoggerFactory? loggerFactory = null) : base(textWriter)
    {
        _logger = loggerFactory?.CreateLogger<AnalyzeJsonWriter>() ?? NullLogger<AnalyzeJsonWriter>.Instance;
    }

    public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
    {
        var analyzeResult = (AnalyzeResult)result;

        JsonSerializer jsonSerializer = new();
        jsonSerializer.Formatting = Formatting.Indented;
        if (TextWriter != null)
        {
            jsonSerializer.Serialize(TextWriter, analyzeResult);
        }

        if (autoClose)
        {
            FlushAndClose();
        }
    }

    /// <summary>
    ///     simple wrapper for serializing results for simple tags only during processing
    /// </summary>
    private class TagsFile
    {
        [JsonProperty(PropertyName = "tags")] public string[]? Tags { get; set; }
    }
}