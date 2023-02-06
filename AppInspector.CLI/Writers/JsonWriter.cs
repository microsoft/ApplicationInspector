using System;
using System.IO;
using System.Text.Json;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.CLI.Writers;

internal class JsonWriter : CommandResultsWriter
{
    private readonly ILogger<JsonWriter> _logger;

    internal JsonWriter(TextWriter textWriter, ILoggerFactory? loggerFactory = null) : base(textWriter)
    {
        _logger = loggerFactory?.CreateLogger<JsonWriter>() ?? NullLogger<JsonWriter>.Instance;
    }

    public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            //jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            //jsonSerializer.DefaultValueHandling = DefaultValueHandling.Ignore;
        };

        if (TextWriter is null)
        {
            throw new ArgumentNullException(nameof(TextWriter));
        }

        string? jsonData;
        try
        {
            switch (result)
            {
                case TagDiffResult:
                case ExportTagsResult:
                case VerifyRulesResult:
                    jsonData = JsonSerializer.Serialize(result, options);
                    TextWriter.Write(jsonData);
                    break;
                case PackRulesResult prr:
                    jsonData = JsonSerializer.Serialize(prr.Rules, options);
                    TextWriter.Write(jsonData);
                    break;
                default:
                    throw new Exception("Unexpected object type for json writer");
            }
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
}