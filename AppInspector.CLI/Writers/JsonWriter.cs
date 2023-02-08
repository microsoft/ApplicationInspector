using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.CLI.Writers;

internal class JsonWriter : CommandResultsWriter
{
    private readonly ILogger<JsonWriter> _logger;

    internal JsonWriter(StreamWriter streamWriter, ILoggerFactory? loggerFactory = null) : base(streamWriter)
    {
        _logger = loggerFactory?.CreateLogger<JsonWriter>() ?? NullLogger<JsonWriter>.Instance;
    }

    public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,            
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault, // The WhenWritingDefault setting also prevents serialization of null-value reference type and nullable value type properties.
            // jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            // jsonSerializer.DefaultValueHandling = DefaultValueHandling.Ignore;
        };

        if (StreamWriter is null)
        {
            throw new ArgumentNullException(nameof(TextWriter));
        }

        try
        {
            switch (result)
            {
                case TagDiffResult:
                case ExportTagsResult:
                case VerifyRulesResult:
                    JsonSerializer.Serialize(StreamWriter.BaseStream, result, options);
                    break;
                case PackRulesResult prr:
                    JsonSerializer.Serialize(StreamWriter.BaseStream, prr.Rules, options);          
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