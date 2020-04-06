﻿using System;
using System.Collections.Generic;
using System.IO;

namespace ApplicationInspector.UnitTest.Misc
{
    public static class Helper
    {
        public enum AppPath { basePath, testSource, testRules, testOutput, defaultRules, appInspectorCLI };

        private static string _basePath;
        static private string GetBaseAppPath()
        {
            if (!String.IsNullOrEmpty(_basePath))
            {
                return _basePath;
            }

            _basePath = Path.GetFullPath(System.AppContext.BaseDirectory);
            return _basePath;
        }



        static public string GetPath(AppPath pathType)
        {
            string result = "";
            switch (pathType)
            {
                case AppPath.basePath:
                    result = GetBaseAppPath();
                    break;
                case AppPath.testSource:
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "UnitTest.Commands", "source");
                    break;
                case AppPath.testRules://Packrules default output use
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "UnitTest.Commands", "customrules");
                    break;

                case AppPath.testOutput://Packrules default output use
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "UnitTest.Commands", "output");
                    break;
                case AppPath.defaultRules:
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector", "rules");
                    break;
                case AppPath.appInspectorCLI:
#if DEBUG
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector.CLI", "bin", "debug", "netcoreapp3.1", "applicationinspector.cli.exe");
#else
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector".CLI", "bin", "release", "netcoreapp3.1", "applicationinspector.cli.exe");
#endif
                    break;
            }

            result = Path.GetFullPath(result);
            return result;
        }



        static public List<string> GetTagsFromFile(string[] contentLines)
        {
            List<string> results = new List<string>();

            int i;
            for (i = 0; i < contentLines.Length; i++)
            {
                if (contentLines[i].Contains("[UniqueTags]"))
                {
                    break;
                }
            }

            i++;//get past marker
            while (!contentLines[i].Contains("Select Counters"))
            {
                results.Add(contentLines[i++]);
                if (i > contentLines.Length)
                {
                    break;
                }
            }

            return results;
        }
    }
}
