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
using System.Runtime.InteropServices;

namespace Microsoft.ApplicationInspector.Commands
{
    //Miscellenous common methods needed from several places throughout
    public static class Utils
    {
        public enum ExitCode
        {
            Success = 0,
            PartialFail = 1,
            CriticalError = 2
        }

        private static string? _basePath;
        public static string? LogFilePath { get; set; } //used to capture and report log path for console messages
        public static Logger? Logger { get; set; }

        public enum AppPath { basePath, defaultRulesSrc, defaultRulesPackedFile, defaultLog, tagGroupPref, tagCounterPref };

        public static string GetVersionString()
        {
            return string.Format("Microsoft Application Inspector {0}", GetVersion());
        }

        public static string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion;
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
                    result = Path.Combine(GetBaseAppPath(), "appinspector.log.txt");
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

        public static void OpenBrowser(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                WriteOnce.Log?.Error("Bad url for OpenBrowser method");
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
                    WriteOnce.General(MsgHelp.GetString(MsgHelp.ID.BROWSER_START_SUCCESS));
                }
                catch (Exception)
                {
                    WriteOnce.General(MsgHelp.GetString(MsgHelp.ID.BROWSER_START_FAIL));//soft error
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROWSER")))
                {
                    try
                    {
                        Process.Start("xdg-open", "\"" + url + "\"");
                        WriteOnce.General(MsgHelp.GetString(MsgHelp.ID.BROWSER_START_SUCCESS));
                    }
                    catch (Exception)
                    {
                        WriteOnce.General(MsgHelp.GetString(MsgHelp.ID.BROWSER_START_FAIL));//soft error
                    }
                }
                else
                {
                    WriteOnce.General(MsgHelp.GetString(MsgHelp.ID.BROWSER_ENVIRONMENT_VAR));
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    Process.Start("open", "\"" + url + "\"");
                    WriteOnce.General(MsgHelp.GetString(MsgHelp.ID.BROWSER_START_SUCCESS));
                }
                catch (Exception)
                {
                    WriteOnce.General(MsgHelp.GetString(MsgHelp.ID.BROWSER_START_FAIL));//soft error
                }
            }
        }

        /// <summary>
        /// For use when logging is needed and was not called via CLI
        /// </summary>
        /// <returns></returns>
        public static Logger SetupLogging()
        {
            CommandOptions opts = new CommandOptions();//defaults used

            return SetupLogging(opts);
        }

        /// <summary>
        /// Setup application inspector logging; 1 file per process
        /// </summary>
        /// <param name="opts"></param>
        /// <returns></returns>
        public static Logger SetupLogging(CommandOptions opts, bool onErrorConsole = false)
        {
            //prevent being called again if already set unless closed first
            if (Logger != null)
            {
                return Logger;
            }

            LoggingConfiguration config = LogManager.Configuration;
            if (config == null)//fix #179 to prevent overwrite of caller config...i.e. just add ours
            {
                config = new LoggingConfiguration();
            }

            if (string.IsNullOrEmpty(opts.LogFilePath))
            {
                opts.LogFilePath = Utils.GetPath(Utils.AppPath.defaultLog);
            }

            //clean up previous for convenience in reading
            if (File.Exists(opts.LogFilePath))
            {
                // Read the file and display it line by line.
                StreamReader file = new StreamReader(opts.LogFilePath);
                string line = file?.ReadLine() ?? "";
                file?.Close();
                if (!string.IsNullOrEmpty(line))
                {
                    if (line.Contains("AppInsLog"))//prevent file other than our logs from deletion
                    {
                        File.Delete(opts.LogFilePath);
                    }
                    else
                    {
                        if (Utils.CLIExecutionContext && onErrorConsole)
                        {
                            WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_LOG_PATH, opts.LogFilePath), true, WriteOnce.ConsoleVerbosity.Low, false);
                        }

                        throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_LOG_PATH, opts.LogFilePath));
                    }
                }
            }
            else
            {
                try
                {
                    File.WriteAllText(opts.LogFilePath, "");//verify log file path is writable
                }
                catch (Exception e)
                {
                    WriteOnce.SafeLog(e.Message + "\n" + e.StackTrace, NLog.LogLevel.Error);
                    if (Utils.CLIExecutionContext && onErrorConsole)
                    {
                        WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, opts.LogFilePath), true, WriteOnce.ConsoleVerbosity.Low, false);
                    }

                    throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, opts.LogFilePath)));
                }
            }

            LogLevel log_level = LogLevel.Error;//default
            if (string.IsNullOrEmpty(opts.LogFileLevel))
            {
                opts.LogFileLevel = "Error";
            }

            try
            {
                log_level = LogLevel.FromString(opts.LogFileLevel);
            }
            catch (Exception)
            {
                if (Utils.CLIExecutionContext && onErrorConsole)
                {
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-v"), true, WriteOnce.ConsoleVerbosity.Low, false);
                }

                throw new OpException((MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-v")));
            }

            using (var fileTarget = new FileTarget()
            {
                Name = "LogFile",
                FileName = opts.LogFilePath,
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} ${threadid} ${level:uppercase=true} - AppInsLog - ${message}",
                ForceMutexConcurrentWrites = true
            })
            {
                config.AddTarget(fileTarget);
                config.LoggingRules.Add(new LoggingRule("Microsoft.CST.ApplicationInspector", log_level, fileTarget));
            }

            LogFilePath = opts.LogFilePath;//preserve for console path msg

            LogManager.Configuration = config;
            Logger = LogManager.GetLogger("Microsoft.CST.ApplicationInspector");
            return Logger;
        }
    }
}