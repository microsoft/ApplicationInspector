using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.ApplicationInspector.RulesEngine.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     The TypedRuleSet allows you to extend the Application Inspector Rule format with your own custom fields and have
///     them be deserialized. They won't be used in processing, but may be used for additional follow up actions.
/// </summary>
/// <typeparam name="T">The Type of the Rule this set holds. It must inherit from <see cref="Rule" /></typeparam>
public class TypedRuleSet<T> : AbstractRuleSet, IEnumerable<T> where T : Rule
{
    private readonly RuleSchemaProvider? _schemaProvider;
    
    /// <summary>
    ///     Creates instance of TypedRuleSet
    /// </summary>
    public TypedRuleSet(ILoggerFactory? loggerFactory = null, RuleSchemaProvider? schemaProvider = null)
    {
        _logger = loggerFactory?.CreateLogger<TypedRuleSet<T>>() ?? NullLogger<TypedRuleSet<T>>.Instance;
        _schemaProvider = schemaProvider;
    }

    /// <summary>
    ///     Returns an enumerator that iterates through the AI Formatted <see cref="Rule" />.
    /// </summary>
    /// <returns> Enumerator </returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return AppInspectorRulesAsEnumerableT().GetEnumerator();
    }

    /// <summary>
    ///     Returns an enumerator that iterates through the AI Formatted <see cref="Rule" />.
    /// </summary>
    /// <returns> Enumerator </returns>
    public IEnumerator GetEnumerator()
    {
        return AppInspectorRulesAsEnumerableT().GetEnumerator();
    }

    /// <summary>
    ///     Returns the set of rules as an <see cref="IEnumerable{T}" />
    /// </summary>
    /// <returns></returns>
    private IEnumerable<T> AppInspectorRulesAsEnumerableT()
    {
        foreach (var rule in _rules)
            if (rule is T ruleAsT)
            {
                yield return ruleAsT;
            }
    }


    /// <summary>
    ///     Load rules from a file or directory
    /// </summary>
    /// <param name="path"> File or directory path containing rules</param>
    /// <param name="tag"> Tag for the rules </param>
    /// <exception cref="ArgumentException">Thrown if the filename is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown if the specified file cannot be found on the file system</exception>
    public void AddPath(string path, string? tag = null)
    {
        if (Directory.Exists(path))
        {
            AddDirectory(path, tag);
        }
        else if (File.Exists(path))
        {
            AddFile(path, tag);
        }
        else
        {
            throw new ArgumentException("The path must exist.", nameof(path));
        }
    }

    /// <summary>
    ///     Parse a directory with rule files and attempts to load all .json files in the directory as rules
    /// </summary>
    /// <param name="path"> Path to rules folder </param>
    /// <param name="tag"> Tag for the rules </param>
    /// <exception cref="ArgumentException">Thrown if the filename is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if the specified file cannot be found on the file system</exception>
    public void AddDirectory(string path, string? tag = null)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException();
        }

        foreach (var filename in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            AddFile(filename, tag);
    }

    /// <summary>
    ///     Load rules from a file
    /// </summary>
    /// <param name="filename"> Filename with rules </param>
    /// <param name="tag"> Tag for the rules </param>
    /// <exception cref="ArgumentException">Thrown if the filename is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown if the specified file cannot be found on the file system</exception>
    public void AddFile(string? filename, string? tag = null)
    {
        if (string.IsNullOrEmpty(filename))
        {
            throw new ArgumentException(null, nameof(filename));
        }

        if (!File.Exists(filename))
        {
            throw new FileNotFoundException();
        }

        using var file = File.OpenText(filename);
        AddString(file.ReadToEnd(), filename, tag);
    }

    /// <summary>
    ///     Load rules from JSON string
    /// </summary>
    /// <param name="jsonString"> JSON string </param>
    /// <param name="sourceName"> Name of the source (file, stream, etc..) </param>
    /// <param name="tag">Additional runtime tag for the rules </param>
    /// <returns>If the rules were added successfully</returns>
    public void AddString(string jsonString, string sourceName, string? tag = null)
    {
        if (StringToRules(jsonString ?? string.Empty, sourceName ?? string.Empty, tag) is { } deserializedList)
        {
            AddRange(deserializedList);
        }
    }

    /// <summary>
    ///     Adds the elements of the collection to the Ruleset
    /// </summary>
    /// <param name="collection"> Collection of rules </param>
    public void AddRange(IEnumerable<T>? collection)
    {
        foreach (var rule in collection ?? Array.Empty<T>()) AddRule(rule);
    }

    /// <summary>
    ///     Add rule into Ruleset
    /// </summary>
    /// <param name="rule"> </param>
    public void AddRule(T rule)
    {
        if (AppInspectorRuleToOatRule(rule) is { } oatRule)
        {
            _logger.LogTrace("Attempting to add rule: {RuleId}:{RuleName}", rule.Id, rule.Name);
            _oatRules.Add(oatRule);
        }
        else
        {
            _logger.LogError(
                "Rule '{RuleId}:{RuleName}' could not be converted into an OAT rule. There may be message in the logs indicating why. You can  run rule verification to identify the issue",
                rule.Id, rule.Name);
        }
    }

    /// <summary>
    ///     Deserialize a string into rules and enumerate them.
    /// </summary>
    /// <param name="jsonString"></param>
    /// <param name="sourceName"></param>
    /// <param name="tag">Add an additional tag to the rules when added.</param>
    /// <exception cref="System.Text.JsonException">
    ///     Thrown if the specified json string cannot be deserialized as a
    ///     <see cref="IEnumerable{T}" />
    /// </exception>
    /// <returns></returns>
    internal IEnumerable<T> StringToRules(string jsonString, string sourceName, string? tag = null)
    {
        // Validate original JSON with schema if schema provider is available
        SchemaValidationResult? schemaResult = null;
        if (_schemaProvider != null)
        {
            try
            {
                schemaResult = _schemaProvider.ValidateJson(jsonString);
                if (!schemaResult.IsValid)
                {
                    _logger.LogWarning("Schema validation failed for '{0}': {1}", 
                        sourceName, 
                        string.Join("; ", schemaResult.Errors.Select(e => $"{e.ErrorType}: {e.Message} at {e.Path}")));
                }
            }
            catch (Exception ex)
            {
                schemaResult = new SchemaValidationResult
                {
                    IsValid = false,
                    Errors = new List<SchemaValidationError>
                    {
                        new SchemaValidationError
                        {
                            Message = $"Schema validation error: {ex.Message}",
                            Path = "",
                            ErrorType = "SchemaValidationException"
                        }
                    }
                };
                _logger.LogWarning("Schema validation error for '{0}': {1}", sourceName, ex.Message);
            }
        }

        List<T>? ruleList;
        try
        {
            var options = new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                
            };
            ruleList = JsonSerializer.Deserialize<List<T>>(jsonString, options);
        }
        catch (JsonException jsonSerializationException)
        {
            _logger.LogError("Failed to deserialize '{0}' at Line {1} Column {2}", sourceName,
                jsonSerializationException.LineNumber, jsonSerializationException.BytePositionInLine);
            throw;
        }

        if (ruleList is not null)
        {
            foreach (var r in ruleList)
            {
                r.Source = sourceName;
                r.RuntimeTag = tag;
                // Store the schema validation result from the original JSON
                r.SchemaValidationResult = schemaResult;
                yield return r;
            }
        }
    }
}