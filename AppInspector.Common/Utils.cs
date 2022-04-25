namespace Microsoft.ApplicationInspector.Common
{
    using System.IO;
    using System.Reflection;

    public static class Utils
    {
        public enum ExitCode
        {
            Success = 0,
            PartialFail = 1,
            CriticalError = 2
        }

        private static string? _basePath;

        public enum AppPath { basePath, defaultRulesSrc, defaultRulesPackedFile, defaultLog, tagGroupPref, tagCounterPref };

        public static string GetVersionString()
        {
            return string.Format("Microsoft Application Inspector {0}", GetVersion());
        }

        public static string GetVersion()
        {
            return (Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false) as AssemblyInformationalVersionAttribute[])?[0].InformationalVersion ?? "Unknown";
        }

        public static bool CLIExecutionContext { get; set; }

        public static string GetPath(AppPath pathType)
        {
            string result = "";
            switch (pathType)
            {
                case AppPath.basePath:
                    result = GetBaseAppPath();
                    break;

                case AppPath.defaultLog:
                    result = "appinspector.log.txt";
                    break;

                case AppPath.defaultRulesSrc://Packrules source use
                    result = Path.GetFullPath(Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector", "rules", "default"));//used to ref project folder
                    break;

                case AppPath.defaultRulesPackedFile://Packrules default output use
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector", "Resources", "defaultRulesPkd.json");//packed default file in project resources
                    break;

                case AppPath.tagGroupPref://CLI use only
                    result = Path.Combine(GetBaseAppPath(), "preferences", "tagreportgroups.json");
                    break;

                case AppPath.tagCounterPref://CLI use only
                    result = Path.Combine(GetBaseAppPath(), "preferences", "tagcounters.json");
                    break;
            }

            result = Path.GetFullPath(result);
            return result;
        }

        private static string GetBaseAppPath()
        {
            if (!string.IsNullOrEmpty(_basePath))
            {
                return _basePath;
            }

            _basePath = Path.GetFullPath(System.AppContext.BaseDirectory);
            return _basePath;
        }
    }
}
