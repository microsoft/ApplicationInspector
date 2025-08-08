// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine.Processors;

/// <summary>
/// Handles XPath queries on XML content
/// </summary>
internal class XPathProcessor : IPathProcessor
{
    private readonly string _content;
    private readonly ILogger _logger;
    private readonly string? _filePath;
    private readonly List<int> _lineStarts;
    private XDocument? _xmlDocument;
    private bool _triedToParse;
    private readonly object _lock = new();

    private const int MaxAttributeSearchDistance = 500;

    public XPathProcessor(string content, ILoggerFactory? loggerFactory, string? filePath, List<int> lineStarts)
    {
        _content = content;
        _filePath = filePath;
        _lineStarts = lineStarts;
        _logger = loggerFactory?.CreateLogger<XPathProcessor>() ?? NullLogger<XPathProcessor>.Instance;
    }

    public IEnumerable<(string value, Boundary location)> GetMatches(string path, Dictionary<string, string>? namespaces = null)
    {
        EnsureParsed();
        if (_xmlDocument is null)
        {
            yield break;
        }

        IXmlNamespaceResolver? nsResolver = null;
        if (namespaces != null && namespaces.Any())
        {
            var nt = new NameTable();
            var nsmgr = new XmlNamespaceManager(nt);
            foreach (var pair in namespaces)
            {
                try { nsmgr.AddNamespace(pair.Key, pair.Value); }
                catch (Exception ex) { _logger.LogWarning("Failed to add namespace prefix '{0}' for '{1}': {2}", pair.Key, pair.Value, ex.Message); }
            }
            nsResolver = nsmgr;
        }

        object evalResult;
        try
        {
            evalResult = nsResolver is null ? _xmlDocument.XPathEvaluate(path) : _xmlDocument.XPathEvaluate(path, nsResolver);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to evaluate XPath '{0}' against file {1}: {2}", path, _filePath ?? "unknown", e.Message);
            yield break;
        }

        int PosFromXObject(XObject xobj)
        {
            if (xobj is IXmlLineInfo li && li.HasLineInfo())
            {
                return CalculatePositionFromLineInfo(li.LineNumber, li.LinePosition);
            }
            return 0;
        }

        if (evalResult is IEnumerable enumerable && evalResult is not string)
        {
            foreach (var item in enumerable)
            {
                if (item is null) continue;
                switch (item)
                {
                    case XAttribute attr:
                    {
                        var value = attr.Value ?? string.Empty;
                        if (value.Length == 0) break;
                        var basePos = PosFromXObject(attr);
                        var attrName = attr.Name.LocalName;
                        var prefix = attr.Parent?.GetPrefixOfNamespace(attr.Name.Namespace);
                        if (!string.IsNullOrEmpty(prefix)) attrName = prefix + ":" + attrName;
                        var valueStart = FindAttributeValuePosition(attrName, value, basePos);
                        var res = CreateResultBoundary(value, valueStart);
                        if (res.HasValue) yield return res.Value;
                        break;
                    }
                    case XElement el:
                    {
                        var value = el.Value ?? string.Empty;
                        if (value.Length == 0) break;
                        var basePos = PosFromXObject(el);
                        var textStart = FindElementTextPosition(value, basePos);
                        var res = CreateResultBoundary(value, textStart);
                        if (res.HasValue) yield return res.Value;
                        break;
                    }
                    case XCData cdata:
                    {
                        var value = cdata.Value ?? string.Empty;
                        if (value.Length == 0) break;
                        var basePos = PosFromXObject(cdata);
                        var start = FindValueInProximity(basePos, value);
                        var res = CreateResultBoundary(value, start);
                        if (res.HasValue) yield return res.Value;
                        break;
                    }
                    case XText xt:
                    {
                        var value = xt.Value ?? string.Empty;
                        if (value.Length == 0) break;
                        var basePos = PosFromXObject(xt);
                        var start = FindValueInProximity(basePos, value);
                        var res = CreateResultBoundary(value, start);
                        if (res.HasValue) yield return res.Value;
                        break;
                    }
                    case XComment comment:
                    {
                        var value = comment.Value ?? string.Empty;
                        if (value.Length == 0) break;
                        var basePos = PosFromXObject(comment);
                        var start = FindValueInProximity(basePos, value);
                        var res = CreateResultBoundary(value, start);
                        if (res.HasValue) yield return res.Value;
                        break;
                    }
                    case XProcessingInstruction pi:
                    {
                        var value = pi.Data ?? string.Empty;
                        if (value.Length == 0) break;
                        var basePos = PosFromXObject(pi);
                        var start = FindValueInProximity(basePos, value);
                        var res = CreateResultBoundary(value, start);
                        if (res.HasValue) yield return res.Value;
                        break;
                    }
                    case XObject xobj:
                    {
                        var s = xobj is XNode xn ? xn.ToString(SaveOptions.DisableFormatting) : (xobj.ToString() ?? string.Empty);
                        if (!string.IsNullOrEmpty(s))
                        {
                            var basePos = PosFromXObject(xobj);
                            var start = FindValueInProximity(basePos, s);
                            var res = CreateResultBoundary(s, start);
                            if (res.HasValue) yield return res.Value;
                        }
                        break;
                    }
                    default:
                    {
                        var s = item.ToString();
                        if (!string.IsNullOrEmpty(s))
                        {
                            var start = FindValueInProximity(0, s);
                            var res = CreateResultBoundary(s, start);
                            if (res.HasValue) yield return res.Value;
                        }
                        else
                        {
                            _logger.LogDebug("Unsupported XPath result type {0} in file {1}.", item.GetType().FullName, _filePath ?? "unknown");
                        }
                        break;
                    }
                }
            }
            yield break;
        }

        string? scalar = evalResult switch
        {
            null => null,
            string s => s,
            bool b => b ? "true" : "false",
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => evalResult.ToString()
        };

        if (!string.IsNullOrEmpty(scalar))
        {
            var idx = _content.IndexOf(scalar, System.StringComparison.Ordinal);
            if (idx < 0)
            {
                idx = FindValueInProximity(0, scalar);
            }
            var res = CreateResultBoundary(scalar, idx);
            if (res.HasValue) yield return res.Value;
        }
    }

    private void EnsureParsed()
    {
        lock (_lock)
        {
            if (_triedToParse) return;
            try
            {
                _triedToParse = true;
                _xmlDocument = XDocument.Parse(_content, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to parse {1} as a XML document: {0}", e.Message, _filePath);
                _xmlDocument = null;
            }
        }
    }

    private int CalculatePositionFromLineInfo(int lineNumber, int linePosition)
    {
        if (lineNumber <= 0 || lineNumber > _lineStarts.Count)
        {
            return 0;
        }
        var lineStartIndex = _lineStarts[lineNumber - 1];
        return lineStartIndex + Math.Max(0, linePosition - 1);
    }

    private (string, Boundary)? CreateResultBoundary(string value, int position)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return (value, new Boundary { Index = position, Length = value.Length });
    }

    private int FindAttributeValuePosition(string attributeName, string value, int approximatePosition)
    {
        if (string.IsNullOrEmpty(attributeName) || string.IsNullOrEmpty(value)) return approximatePosition;
        var searchPattern = $"{attributeName}=";
        var patternIndex = _content.IndexOf(searchPattern, approximatePosition, System.StringComparison.Ordinal);
        if (patternIndex == -1)
        {
            var lineStart = GetLineBoundary(approximatePosition).Index;
            var searchStart = Math.Max(0, lineStart);
            var searchLength = Math.Min(_content.Length - searchStart, approximatePosition - searchStart + MaxAttributeSearchDistance);
            patternIndex = _content.IndexOf(searchPattern, searchStart, searchLength, System.StringComparison.Ordinal);
        }
        if (patternIndex >= 0)
        {
            var quoteIndex = patternIndex + searchPattern.Length;
            while (quoteIndex < _content.Length && char.IsWhiteSpace(_content[quoteIndex])) quoteIndex++;
            if (quoteIndex < _content.Length && (_content[quoteIndex] == '"' || _content[quoteIndex] == '\''))
            {
                var valueStart = quoteIndex + 1;
                if (valueStart + value.Length <= _content.Length && _content.AsSpan(valueStart, value.Length).SequenceEqual(value.AsSpan()))
                {
                    return valueStart;
                }
            }
        }
        return FindValueInProximity(approximatePosition, value);
    }

    private int FindValueInProximity(int approximatePosition, string value)
    {
        if (string.IsNullOrEmpty(value)) return approximatePosition;
        var forwardIndex = _content.IndexOf(value, approximatePosition, System.StringComparison.Ordinal);
        var backwardIndex = _content.LastIndexOf(value, approximatePosition, System.StringComparison.Ordinal);
        if (forwardIndex >= 0 && backwardIndex >= 0)
        {
            var forwardDistance = forwardIndex - approximatePosition;
            var backwardDistance = approximatePosition - backwardIndex;
            return forwardDistance <= backwardDistance ? forwardIndex : backwardIndex;
        }
        if (forwardIndex >= 0) return forwardIndex;
        if (backwardIndex >= 0) return backwardIndex;
        return approximatePosition;
    }

    private int FindElementTextPosition(string value, int approximatePosition)
    {
        if (string.IsNullOrEmpty(value)) return approximatePosition;
        var startTagClose = _content.IndexOf('>', approximatePosition);
        if (startTagClose >= 0 && startTagClose + 1 < _content.Length)
        {
            var idxAfterTag = _content.IndexOf(value, startTagClose + 1, System.StringComparison.Ordinal);
            if (idxAfterTag >= 0) return idxAfterTag;
            var normalizedIdx = IndexOfValueWithCrlfNormalization(startTagClose + 1, value);
            if (normalizedIdx >= 0) return normalizedIdx;
        }
        var exact = FindExactTextPosition(approximatePosition, value);
        if (exact >= 0) return exact;
        var normalized = IndexOfValueWithCrlfNormalization(Math.Max(0, approximatePosition - 1), value);
        if (normalized >= 0) return normalized;
        return approximatePosition;
    }

    private int IndexOfValueWithCrlfNormalization(int startIndex, string value)
    {
        if (string.IsNullOrEmpty(value)) return -1;
        int contentLen = _content.Length;
        for (int i = Math.Max(0, startIndex); i < contentLen; i++)
        {
            int j = 0; int k = i; int firstMatchIndex = -1;
            while (j < value.Length && k < contentLen)
            {
                char hc = _content[k];
                if (hc == '\r') { k++; continue; }
                char vc = value[j];
                if (hc != vc) break;
                if (firstMatchIndex == -1) firstMatchIndex = k;
                j++; k++;
            }
            if (j == value.Length) return firstMatchIndex >= 0 ? firstMatchIndex : i;
        }
        return -1;
    }

    private int FindExactTextPosition(int approximatePosition, string value)
    {
        if (string.IsNullOrEmpty(value)) return approximatePosition;
        var idx = FindValueInProximity(approximatePosition, value);
        if (idx >= 0) return idx;
        var normalizedIdx = IndexOfValueWithCrlfNormalization(Math.Max(0, approximatePosition - 1), value);
        return normalizedIdx >= 0 ? normalizedIdx : approximatePosition;
    }

    private Boundary GetLineBoundary(int index)
    {
        Boundary result = new();
        // We do not have line ends, so we approximate by using line starts only (caller may adjust if needed)
        // This is a simplified version; original TextContainer had precise line ends.
        // For attribute search fallback we only need start index.
        for (var i = 0; i < _lineStarts.Count - 1; i++)
        {
            if (_lineStarts[i + 1] > index)
            {
                result.Index = _lineStarts[i];
                result.Length = _lineStarts[i + 1] - _lineStarts[i];
                return result;
            }
        }
        if (_lineStarts.Count > 0)
        {
            result.Index = _lineStarts[^1];
            result.Length = _content.Length - result.Index;
        }
        return result;
    }
}