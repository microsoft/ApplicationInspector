using System;
using System.Collections.Generic;
using Microsoft.ApplicationInspector.RulesEngine;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
/// Shared helper methods for creating rules in tests
/// </summary>
public static class RuleTestHelpers
{
    /// <summary>
    /// Creates a RuleProcessor with standard test options
    /// </summary>
    /// <param name="rules">The RuleSet to use</param>
    /// <returns>RuleProcessor configured for testing</returns>
    public static Microsoft.ApplicationInspector.RulesEngine.RuleProcessor CreateProcessor(RuleSet rules)
    {
        return new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions { AllowAllTagsInBuildFiles = true });
    }

    /// <summary>
    /// Builds a RuleSet with a single rule and returns a configured processor
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <param name="pattern">Pattern to match</param>
    /// <param name="type">Pattern type (string, substring, regex, etc.)</param>
    /// <param name="xpath">XPath expression</param>
    /// <param name="namespacesJson">Optional JSON string for XPath namespaces</param>
    /// <param name="name">Optional rule name (defaults to "{id} Test")</param>
    /// <returns>RuleProcessor with the specified rule</returns>
    public static Microsoft.ApplicationInspector.RulesEngine.RuleProcessor BuildRuleAndProcessor(string id, string pattern, string type, string xpath, string? namespacesJson = null, string? name = null)
    {
        var rules = BuildRule(id, pattern, type, xpath, namespacesJson, name);
        return CreateProcessor(rules);
    }

    /// <summary>
    /// Builds a RuleSet with a single rule and returns a configured processor
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <param name="pattern">Pattern to match</param>
    /// <param name="type">Pattern type (string, substring, regex, etc.)</param>
    /// <param name="xpath">XPath expression</param>
    /// <param name="namespaces">Dictionary of namespace prefixes to URIs</param>
    /// <param name="name">Optional rule name (defaults to "{id} Test")</param>
    /// <returns>RuleProcessor with the specified rule</returns>
    public static Microsoft.ApplicationInspector.RulesEngine.RuleProcessor BuildRuleAndProcessor(string id, string pattern, string type, string xpath, Dictionary<string, string> namespaces, string? name = null)
    {
        var rules = BuildRule(id, pattern, type, xpath, namespaces, name);
        return CreateProcessor(rules);
    }
    /// <summary>
    /// Builds a RuleSet with a single rule containing a single pattern
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <param name="pattern">Pattern to match</param>
    /// <param name="type">Pattern type (string, substring, regex, etc.)</param>
    /// <param name="xpath">XPath expression</param>
    /// <param name="namespacesJson">Optional JSON string for XPath namespaces</param>
    /// <param name="name">Optional rule name (defaults to "{id} Test")</param>
    /// <returns>RuleSet containing the specified rule</returns>
    public static RuleSet BuildRule(string id, string pattern, string type, string xpath, string? namespacesJson = null, string? name = null)
    {
        var xpathNamespaces = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(namespacesJson))
        {
            // Parse simple namespace JSON if provided (for backward compatibility)
            // This is a simplified parser for the test scenarios
            try
            {
                var namespaces = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(namespacesJson);
                if (namespaces != null)
                {
                    xpathNamespaces = namespaces;
                }
            }
            catch
            {
                // If parsing fails, use empty dictionary
            }
        }

        // Parse the pattern type from string to enum
        if (!Enum.TryParse<PatternType>(type, true, out var patternType))
        {
            throw new ArgumentException($"Invalid pattern type: {type}");
        }

        var searchPattern = new SearchPattern
        {
            Pattern = pattern,
            PatternType = patternType,
            XPaths = new[] { xpath },
            XPathNamespaces = xpathNamespaces
        };

        var rule = new Rule
        {
            Id = id,
            Name = name ?? id + " Test",
            Patterns = new[] { searchPattern }
        };

        var rs = new RuleSet();
        rs.AddRule(rule);
        return rs;
    }

    /// <summary>
    /// Builds a RuleSet with a single rule containing a single pattern with XPath namespaces
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <param name="pattern">Pattern to match</param>
    /// <param name="type">Pattern type (string, substring, regex, etc.)</param>
    /// <param name="xpath">XPath expression</param>
    /// <param name="namespaces">Dictionary of namespace prefixes to URIs</param>
    /// <param name="name">Optional rule name (defaults to "{id} Test")</param>
    /// <returns>RuleSet containing the specified rule</returns>
    public static RuleSet BuildRule(string id, string pattern, string type, string xpath, Dictionary<string, string> namespaces, string? name = null)
    {
        // Parse the pattern type from string to enum
        if (!Enum.TryParse<PatternType>(type, true, out var patternType))
        {
            throw new ArgumentException($"Invalid pattern type: {type}");
        }

        var searchPattern = new SearchPattern
        {
            Pattern = pattern,
            PatternType = patternType,
            XPaths = new[] { xpath },
            XPathNamespaces = namespaces ?? new Dictionary<string, string>()
        };

        var rule = new Rule
        {
            Id = id,
            Name = name ?? id + " Test",
            Patterns = new[] { searchPattern }
        };

        var rs = new RuleSet();
        rs.AddRule(rule);
        return rs;
    }
}
