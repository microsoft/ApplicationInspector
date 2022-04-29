// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.ApplicationInspector.Commands
{
    using Microsoft.ApplicationInspector.RulesEngine;
    using Microsoft.Extensions.Logging;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    //Miscellenous common methods needed from several places throughout
    public static class RuleSetUtils
    {
        /// <summary>
        /// Common method of retrieving rules from AppInspector.Commands manifest
        /// </summary>
        /// <param name="loggerFactory">If you want log message, provide a loggerfactory configured to your preferences.</param>
        /// <returns>The default RuleSet embedded in the App Inspector binary.</returns>
        public static RuleSet GetDefaultRuleSet(ILoggerFactory? loggerFactory = null)
        {
            RuleSet ruleSet = new(loggerFactory);
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (string resName in resNames.Where(x => x.StartsWith("Microsoft.ApplicationInspector.Commands.rules.default")))
            {
                Stream? resource = assembly.GetManifestResourceStream(resName);
                using StreamReader file = new(resource ?? new MemoryStream());
                ruleSet.AddString(file.ReadToEnd(), resName, null);
            }

            return ruleSet;
        }
    }
}