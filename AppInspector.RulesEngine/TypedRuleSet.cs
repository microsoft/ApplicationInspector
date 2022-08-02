using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
/// The TypedRuleSet allows you to extend the Application Inspector Rule format with your own custom fields and have them be deserialized. They won't be used in processing, but may be used for additional follow up actions.
/// </summary>
/// <typeparam name="T">The Type of the Rule this set holds. It must inherit from <see cref="Rule"/></typeparam>
public class TypedRuleSet<T> : AbstractRuleSet, IEnumerable<T>  where T : Rule
{
    /// <summary>
    ///     Creates instance of TypedRuleSet
    /// </summary>
    public TypedRuleSet(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<TypedRuleSet<T>>() ?? NullLogger<TypedRuleSet<T>>.Instance;
    }

    /// <summary>
    ///     Returns an enumerator that iterates through the AI Formatted <see cref="Rule"/>.
    /// </summary>
    /// <returns> Enumerator </returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => AppInspectorRulesAsEnumerableT().GetEnumerator();
    
    /// <summary>
    ///     Returns an enumerator that iterates through the AI Formatted <see cref="Rule"/>.
    /// </summary>
    /// <returns> Enumerator </returns>
    public IEnumerator GetEnumerator() => AppInspectorRulesAsEnumerableT().GetEnumerator();

    /// <summary>
    /// Returns the set of rules as an <see cref="IEnumerable{T}"/>
    /// </summary>
    /// <returns></returns>
    private IEnumerable<T> AppInspectorRulesAsEnumerableT()
    {
        foreach (var rule in _rules)
        {
            if (rule is T ruleAsT)
            {
                yield return ruleAsT;
            }
        }
    }
    
    /// <summary>
    /// Deserialize a string into rules and enumerate them.
    /// </summary>
    /// <param name="jsonString"></param>
    /// <param name="sourceName"></param>
    /// <param name="tag">Add an additional tag to the rules when added.</param>
    /// <returns></returns>
    internal override IEnumerable<Rule> StringToRules(string jsonString, string sourceName, string? tag = null)
    {
        List<T>? ruleList = null;
        try
        {
            ruleList = JsonConvert.DeserializeObject<List<T>>(jsonString);
        }
        catch (JsonSerializationException jsonSerializationException)
        {
            _logger.LogError("Failed to deserialize '{0}' at Line {1} Column {2}", sourceName, jsonSerializationException.LineNumber, jsonSerializationException.LinePosition);
            throw;
        }
        if (ruleList is not null)
        {
            foreach (T r in ruleList)
            {
                r.Source = sourceName;
                r.RuntimeTag = tag;
                yield return r;
            }
        }
    }
}