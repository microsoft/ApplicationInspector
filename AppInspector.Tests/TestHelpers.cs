using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.ApplicationInspector.CLI;
using Microsoft.Extensions.Logging;

namespace AppInspector.Tests
{
    [ExcludeFromCodeCoverage]
    public static class TestHelpers
    {
        public static ILoggerFactory GenerateLoggerFactory(string logName = "testLog.txt", Serilog.Events.LogEventLevel fileLevel = Serilog.Events.LogEventLevel.Verbose, Serilog.Events.LogEventLevel consoleLevel = Serilog.Events.LogEventLevel.Debug) =>
            new LogOptions()
            {
                LogFileLevel = fileLevel,
                LogFilePath = Path.Combine(GetPath(AppPath.testLogOutput), logName),
                ConsoleVerbosityLevel = consoleLevel
            }.GetLoggerFactory();
        public enum AppPath { basePath, testSource, testRules, testOutput, defaultRules, appInspectorCLI, testLogOutput };

        private static string _basePath = string.Empty;

        private static string GetBaseAppPath()
        {
            if (!string.IsNullOrEmpty(_basePath))
            {
                return _basePath;
            }

            _basePath = Path.GetFullPath(AppContext.BaseDirectory);
            return _basePath;
        }

        public static string GetPath(AppPath pathType)
        {
            string result = "";
            switch (pathType)
            {
                case AppPath.basePath:
                    result = GetBaseAppPath();
                    break;

                case AppPath.testSource:
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector.Tests", "source");
                    break;

                case AppPath.testRules://Packrules default output use
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector.Tests", "customrules");
                    break;

                case AppPath.testOutput://Packrules default output use
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector.Tests", "output");
                    break;

                case AppPath.defaultRules:
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector", "rules");
                    break;

                case AppPath.appInspectorCLI:
#if DEBUG
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector.CLI", "bin", "debug", "net6.0", "applicationinspector.cli.exe");
#else
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector.CLI", "bin", "release", "net6.0", "applicationinspector.cli.exe");
#endif
                    break;
                case AppPath.testLogOutput:
                    result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector.Tests", "logs");
                    break;
            }

            result = Path.GetFullPath(result);
            return result;
        }

        public static List<string> GetTagsFromFile(string[] contentLines)
        {
            List<string> results = new();

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

        public static int RunProcess(string appFilePath, string arguments)
        {
            int result = 2;
            using (Process process = new())
            {
                process.StartInfo.FileName = appFilePath;
                process.StartInfo.Arguments = arguments;
                process.Start();
                process.WaitForExit();
                result = process.ExitCode;
            }

            return result;
        }

        public static int RunProcess(string appFilePath, string arguments, out string consoleContent)
        {
            int result;
            using (Process process = new())
            {
                process.StartInfo.FileName = appFilePath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                consoleContent = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                result = process.ExitCode;

                using StreamWriter standardOutput = new(Console.OpenStandardOutput());
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
            }

            return result;
        }
    }
}