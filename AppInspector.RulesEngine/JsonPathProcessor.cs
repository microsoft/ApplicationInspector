// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using JsonCons.JsonPath;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine.Processors;

/// <summary>
/// Handles JsonPath queries on JSON content
/// </summary>
internal class JsonPathProcessor : IPathProcessor
{
    private readonly string _content;
    private readonly ILogger _logger;
    private readonly string? _filePath;
    private JsonDocument? _jsonDocument;
    private bool _triedParse;
    private readonly object _lock = new();

    public JsonPathProcessor(string content, ILoggerFactory? loggerFactory, string? filePath)
    {
        _content = content;
        _filePath = filePath;
        _logger = loggerFactory?.CreateLogger<JsonPathProcessor>() ?? NullLogger<JsonPathProcessor>.Instance;
    }

    public IEnumerable<(string value, Boundary location)> GetMatches(string path, Dictionary<string, string>? _ = null)
    {
        EnsureParsed();
        if (_jsonDocument is null)
        {
            yield break;
        }

        JsonSelector selector;
        try
        {
            selector = JsonSelector.Parse(path);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to parse JsonPath '{0}' for file {1}: {2}", path, _filePath ?? "unknown", e.Message);
            yield break;
        }

        IEnumerable<JsonElement> values;
        try
        {
            values = selector.Select(_jsonDocument.RootElement);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to evaluate JsonPath '{0}' against file {1}: {2}", path, _filePath ?? "unknown", e.Message);
            yield break;
        }

        // Access private index field to approximate location in original string
        var field = typeof(JsonElement).GetField("_idx", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field is null)
        {
            _logger.LogWarning("Failed to access _idx field of JsonElement.");
            yield break;
        }

        foreach (var ele in values)
        {
            if (field.GetValue(ele) is int idx)
            {
                var eleString = ele.ValueKind switch
                {
                    JsonValueKind.False => "false",
                    JsonValueKind.True => "true",
                    _ => ele.ToString()
                };
                if (string.IsNullOrEmpty(eleString)) continue;
                var relativeIndex = _content[idx..].IndexOf(eleString, StringComparison.Ordinal);
                if (relativeIndex < 0) continue; // fallback not found
                var location = new Boundary
                {
                    Index = relativeIndex + idx,
                    Length = eleString.Length
                };
                yield return (eleString, location);
            }
        }
    }

    private void EnsureParsed()
    {
        lock (_lock)
        {
            if (_triedParse) return;
            try
            {
                _triedParse = true;
                _jsonDocument = JsonDocument.Parse(_content);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to parse {1} as a JSON document: {0}", e.Message, _filePath);
                _jsonDocument = null;
            }
        }
    }
}
