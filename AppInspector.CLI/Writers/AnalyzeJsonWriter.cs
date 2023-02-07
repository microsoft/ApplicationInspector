// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json.Serialization;
using System.Text.Json;
using System;

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

    public AnalyzeJsonWriter(StreamWriter textWriter, ILoggerFactory? loggerFactory = null) : base(textWriter)
    {
        _logger = loggerFactory?.CreateLogger<AnalyzeJsonWriter>() ?? NullLogger<AnalyzeJsonWriter>.Instance;
    }

    public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
    {
        var analyzeResult = (AnalyzeResult)result;
        if (StreamWriter == null)
        { 
            throw new ArgumentNullException(nameof(StreamWriter));
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            JsonSerializer.Serialize(StreamWriter.BaseStream, analyzeResult, options);
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Failed to serialize JSON representation of results in memory. {Type} : {Message}",
                e.GetType().Name, e.Message);
            throw;
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
        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }
    }
}