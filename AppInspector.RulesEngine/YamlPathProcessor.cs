// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YamlDotNet.RepresentationModel;
using gfs.YamlDotNet.YamlPath;

namespace Microsoft.ApplicationInspector.RulesEngine.Processors;

/// <summary>
/// Handles YamlPath queries on YAML content
/// </summary>
internal class YamlPathProcessor : IPathProcessor
{
    private readonly string _content;
    private readonly ILogger _logger;
    private readonly string? _filePath;
    private YamlStream? _yamlStream;
    private bool _triedParse;
    private readonly object _lock = new();

    public YamlPathProcessor(string content, ILoggerFactory? loggerFactory, string? filePath)
    {
        _content = content;
        _filePath = filePath;
        _logger = loggerFactory?.CreateLogger<YamlPathProcessor>() ?? NullLogger<YamlPathProcessor>.Instance;
    }

    public IEnumerable<(string value, Boundary location)> GetMatches(string path, Dictionary<string, string>? _ = null)
    {
        EnsureParsed();
        if (!(_yamlStream?.Documents.Count > 0)) yield break;

        var docs = _yamlStream.Documents.ToImmutableArray();
        foreach (var match in docs.Select(d => d.RootNode.Query(path)).SelectMany(m => m))
        {
            var value = match.ToString();
            if (string.IsNullOrEmpty(value)) continue;
            // Casting indexes (YamlDotNet uses long-like index values)
            var start = (int)match.Start.Index;
            var end = (int)match.End.Index;
            var length = Math.Max(0, end - start);
            yield return (value, new Boundary { Index = start, Length = length });
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
                _yamlStream = new YamlStream();
                _yamlStream.Load(new StringReader(_content));
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to parse {1} as a YML document: {0}", e.Message, _filePath);
                _yamlStream = null;
            }
        }
    }
}
