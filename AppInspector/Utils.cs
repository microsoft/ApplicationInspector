// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.ApplicationInspector.RulesEngine;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.ApplicationInspector.Commands
{
    //Miscellenous common methods needed from several places throughout
    static public class Utils
    {
        public enum ExitCode
        {
            Success = 0,
            PartialFail = 1,
            CriticalError = 2
        }

        static string _basePath;
        static public string LogFilePath { get; set; } //used to capture and report log path for console messages
        static public Logger Logger { get; set; }

        public enum AppPath { basePath, defaultRulesSrc, defaultRulesPackedFile, defaultLog, tagGroupPref, tagCounterPref };

        public static string GetVersionString()
        {
            return String.Format("Microsoft Application Inspector {0}", GetVersion());
        }

        public static string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion;
        }

        static public bool CLIExecutionContext { get; set; }

        static public string GetPath(AppPath pathType)
        {
            string result = "";
            switch (pathType)
            {
                case AppPath.basePath:
                    result = GetBaseAppPath();
                    break;
                case AppPath.defaultLog:
                    result = Path.Combine(GetBaseAppPath(), "log.txt");
                    break;
                case AppPath.defaultRulesSrc://Packrules source use
                    result = Path.GetFullPath(Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector", "rules", "default"));//used to ref project folder
                    break;
                case AppPath.defaultRulesPackedFile://Packrules default output use
                    result = Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "AppInspector", "Resources", "defaultRulesPkd.json");//packed default file in project resources
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

        static private string GetBaseAppPath()
        {
            if (!String.IsNullOrEmpty(_basePath))
                return _basePath;

            _basePath = Path.GetFullPath(System.AppContext.BaseDirectory);
            return _basePath;
        }


        /// <summary>
        /// Common method of retrieving rules from AppInspector.Commands manifest
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        static public RuleSet GetDefaultRuleSet(Logger logger = null)
        {
            RuleSet ruleSet = new RuleSet(logger);
            Assembly assembly = Assembly.GetExecutingAssembly();
            string filePath = "Microsoft.ApplicationInspector.Commands.defaultRulesPkd.json";
            Stream resource = assembly.GetManifestResourceStream(filePath);
            using (StreamReader file = new StreamReader(resource))
            {
                ruleSet.AddString(file.ReadToEnd(), filePath, null);
            }

            return ruleSet;
        }



        /// <summary>
        /// Attempt to map application type tags or file type or language to identify
        /// WebApplications, Windows Services, Client Apps, WebServices, Azure Functions etc.
        /// </summary>
        /// <param name="match"></param>
        static public String DetectSolutionType(MatchRecord match)
        {
            string result = "";
            if (match.Issue.Rule.Tags.Any(s => s.Contains("Application.Type")))
            {
                foreach (string tag in match.Issue.Rule.Tags)
                {
                    int index = tag.IndexOf("Application.Type");
                    if (-1 != index)
                    {
                        result = tag.Substring(index + 17);
                        break;
                    }
                }
            }
            else
            {
                switch (match.Filename)
                {
                    case "web.config":
                        result = "Web.Application";
                        break;
                    case "app.config":
                        result = ".NETclient";
                        break;
                    default:
                        switch (Path.GetExtension(match.Filename))
                        {
                            case ".cshtml":
                                result = "Web.Application";
                                break;
                            case ".htm":
                            case ".html":
                            case ".js":
                            case ".ts":
                                result = "Web.Application";
                                break;
                            case "powershell":
                            case "shellscript":
                            case "wincmdscript":
                                result = "script";
                                break;
                            default:
                                switch (match.Language.Name)
                                {
                                    case "ruby":
                                    case "perl":
                                    case "php":
                                        result = "Web.Application";
                                        break;
                                }
                                break;
                        }
                        break;
                }

            }

            return result.ToLower();
        }



        public static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {

                try
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
                    WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_START_SUCCESS));
                }
                catch (Exception)
                {
                    WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_START_FAIL));//soft error
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROWSER")))
                {
                    try
                    {
                        Process.Start("xdg-open", "\"" + url + "\"");
                        WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_START_SUCCESS));
                    }
                    catch (Exception)
                    {
                        WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_START_FAIL));//soft error
                    }
                }
                else
                {
                    WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_ENVIRONMENT_VAR));
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    Process.Start("open", "\"" + url + "\"");
                    WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_START_SUCCESS));
                }
                catch (Exception)
                {
                    WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_START_FAIL));//soft error
                }
            }
        }


        /// <summary>
        /// For use when logging is needed and was not called via CLI
        /// </summary>
        /// <returns></returns>
        public static Logger SetupLogging()
        {
            AllCommandOptions opts = new AllCommandOptions();//defaults used

            return SetupLogging(opts);
        }



        /// <summary>
        /// Setup application inspector logging; 1 file per process
        /// </summary>
        /// <param name="opts"></param>
        /// <returns></returns>
        public static Logger SetupLogging(AllCommandOptions opts)
        {
            //prevent being called again if already set unless closed first
            if (Logger != null)
                return Logger;

            var config = new NLog.Config.LoggingConfiguration();

            if (String.IsNullOrEmpty(opts.LogFilePath))
            {
                opts.LogFilePath = Utils.GetPath(Utils.AppPath.defaultLog);
            }

            //clean up previous for convenience in reading
            if (File.Exists(opts.LogFilePath))
            {
                // Read the file and display it line by line.  
                System.IO.StreamReader file = new System.IO.StreamReader(opts.LogFilePath);
                String line = file.ReadLine();
                file.Close();
                if (line.Contains("AppInsLog"))
                    File.Delete(opts.LogFilePath);
            }

            LogLevel log_level = LogLevel.Error;//default
            if (String.IsNullOrEmpty(opts.LogFileLevel))
                opts.LogFileLevel = "Error";

            try
            {
                log_level = LogLevel.FromString(opts.LogFileLevel);
            }
            catch (Exception)
            {
                throw new Exception((ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-v")));
            }

            using (var fileTarget = new FileTarget()
            {
                Name = "LogFile",
                FileName = opts.LogFilePath,
                Layout = @"${date:universalTime=true:format=s} ${threadid} ${level:uppercase=true} - AppInsLog - ${message}",
                ForceMutexConcurrentWrites = true

            })
            {
                config.AddTarget(fileTarget);
                config.LoggingRules.Add(new LoggingRule("CST.ApplicationInspector", log_level, fileTarget));
            }

            LogFilePath = opts.LogFilePath;//preserve for console path msg

            LogManager.Configuration = config;
            Logger = LogManager.GetLogger("CST.ApplicationInspector");
            return Logger;
        }

    }

}
