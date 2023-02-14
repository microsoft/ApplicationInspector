// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInspector.Commands;

//Miscellenous common methods needed from several places throughout
public static class RuleSetUtils
{
    /// <summary>
    ///     Common method of retrieving rules from AppInspector.Commands manifest
    /// </summary>
    /// <param name="loggerFactory">If you want log message, provide a loggerfactory configured to your preferences.</param>
    /// <returns>The default RuleSet embedded in the App Inspector binary.</returns>
    public static RuleSet GetDefaultRuleSet(ILoggerFactory? loggerFactory = null)
    {
        RuleSet ruleSet = new(loggerFactory);
        var assembly = Assembly.GetExecutingAssembly();
        var resNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        foreach (var resName in resNames.Where(x =>
                     x.StartsWith("Microsoft.ApplicationInspector.Commands.rules.default")))
        {
            var resource = assembly.GetManifestResourceStream(resName);
            using StreamReader file = new(resource ?? new MemoryStream());
            ruleSet.AddString(file.ReadToEnd(), resName);
        }

        return ruleSet;
    }
}