// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.ApplicationInspector.Commands
{
    using Microsoft.ApplicationInspector.RulesEngine;
    using NLog;
    using System.IO;
    using System.Reflection;

    //Miscellenous common methods needed from several places throughout
    public static class RuleSetUtils
    {
        /// <summary>
        /// Common method of retrieving rules from AppInspector.Commands manifest
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static RuleSet GetDefaultRuleSet(Logger? logger = null)
        {
            RuleSet ruleSet = new(logger);
            Assembly assembly = Assembly.GetExecutingAssembly();
            string filePath = "Microsoft.ApplicationInspector.Commands.defaultRulesPkd.json";
            Stream? resource = assembly.GetManifestResourceStream(filePath);
            using (StreamReader file = new(resource ?? new MemoryStream()))
            {
                ruleSet.AddString(file.ReadToEnd(), filePath, null);
            }

            return ruleSet;
        }
    }
}