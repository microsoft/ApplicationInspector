using System;
using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

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
        JsonSerializer jsonSerializer = new();
        jsonSerializer.Formatting = Formatting.Indented;
        jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
        jsonSerializer.DefaultValueHandling = DefaultValueHandling.Ignore;

        if (TextWriter is null)
        {
            throw new ArgumentNullException(nameof(TextWriter));
        }

        switch (result)
        {
            case TagDiffResult:
            case ExportTagsResult:
            case VerifyRulesResult:
                jsonSerializer.Serialize(TextWriter, result);
                break;
            case PackRulesResult prr:
                jsonSerializer.Serialize(TextWriter, prr.Rules);
                break;
            default:
                throw new Exception("Unexpected object type for json writer");
        }

        if (autoClose)
        {
            FlushAndClose();
        }
    }
}