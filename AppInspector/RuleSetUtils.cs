// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.ApplicationInspector.RulesEngine;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Microsoft.ApplicationInspector.Commands
{
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
            RuleSet ruleSet = new RuleSet(logger);
            Assembly assembly = Assembly.GetExecutingAssembly();
            string filePath = "Microsoft.ApplicationInspector.Commands.defaultRulesPkd.json";
            Stream? resource = assembly.GetManifestResourceStream(filePath);
            using (StreamReader file = new StreamReader(resource ?? new MemoryStream()))
            {
                ruleSet.AddString(file.ReadToEnd(), filePath, null);
            }

            return ruleSet;
        }
    }
}