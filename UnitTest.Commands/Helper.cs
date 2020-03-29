using System;
using System.IO;

namespace ApplicationInspector.UnitTest.Commands
{
    public class Helper
    {
        public enum AppPath { basePath, testSource, testRules };

        static string _basePath;
        static private string GetBaseAppPath()
        {
            if (!String.IsNullOrEmpty(_basePath))
                return _basePath;

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
            }

            result = Path.GetFullPath(result);
            return result;
        }
    }
}
