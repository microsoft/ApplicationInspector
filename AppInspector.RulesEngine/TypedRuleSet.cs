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
public class TypedRuleSet<T> : AbstractRuleSet,IEnumerable<T>  where T : Rule
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
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetAppInspectorRules().Select(x => x as T).GetEnumerator();
    
    /// <summary>
    ///     Returns an enumerator that iterates through the AI Formatted <see cref="Rule"/>.
    /// </summary>
    /// <returns> Enumerator </returns>
    public IEnumerator GetEnumerator() => GetAppInspectorRules().GetEnumerator();

    /// <summary>
    /// Deserialize a string into rules and enumerate them.
    /// </summary>
    /// <param name="jsonstring"></param>
    /// <param name="sourcename"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    internal override IEnumerable<Rule> StringToRules(string jsonstring, string sourcename, string? tag = null)
    {
        List<T>? ruleList = JsonConvert.DeserializeObject<List<T>>(jsonstring);
         
        if (ruleList is not null)
        {
            foreach (T r in ruleList)
            {
                r.Source = sourcename;
                r.RuntimeTag = tag ?? "";
                yield return r;
            }
        }
    }
}