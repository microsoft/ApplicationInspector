// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ApplicationInspector.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

static public class ResourceMsg
{
    /// <summary>
    /// Maps enum values to resource strings for ensuring values exists at compile time
    /// </summary>
    public enum ID
    {
        ANALYZE_COMPRESSED_FILETYPE,
        ANALYZE_FILES_PROCESSED_PCNT,
        ANALYZE_NOPATTERNS,
        ANALYZE_NOSUPPORTED_FILETYPES,
        ANALYZE_UNCOMPRESSED_FILETYPE,
        ANALYZE_UNSUPPORTED_COMPR_TYPE,
        ANALYZE_FILESIZE_SKIPPED,
        ANALYZE_OUTPUT_FILE,
        CMD_PREPARING_REPORT,
        CMD_COMPLETED,
        CMD_CRITICAL_FILE_ERR,
        CMD_INVALID_ARG_VALUE,
        CMD_INVALID_FILE_OR_DIR,
        CMD_REPORT_DONE,
        CMD_REQUIRED_ARG_MISSING,
        CMD_RUNNING,
        CMD_INVALID_RULE_PATH,
        CMD_NORULES_SPECIFIED,
        TAGDIFF_NO_TAGS_FOUND,
        TAGDIFF_RESULTS_DIFFER,
        TAGDIFF_RESULTS_GAP,
        TAGDIFF_RESULTS_TEST_TYPE,
        TAGDIFF_SAME_FILE_ARG,
        TAGDIFF_RESULTS_SUCCESSS,
        TAGDIFF_RESULTS_FAIL,
        TAGTEST_RESULTS_NONE,
        TAGTEST_RESULTS_TAGS_FOUND,
        TAGTEST_RESULTS_TAGS_MISSING,
        TAGTEST_RESULTS_TEST_TYPE,
        TAGTEST_RESULTS_SUCCESS,
        TAGTEST_RESULTS_FAIL,
        VERIFY_RULE_FAILED,
        VERIFY_RULES_RESULTS_FAIL,
        VERIFY_RULES_RESULTS_SUCCESS,
        RUNTIME_ERROR_NAMED,
        RUNTIME_ERROR_UNNAMED
    };


}

static public class Helper
{
    static string _basePath;
    public enum AppPath { basePath, defaultRules, defaultLog, tagGroupPref, tagCounterPref };

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
            case AppPath.defaultRules:
                result = Path.Combine(GetBaseAppPath(), "rules", "default");
                break;
            case AppPath.tagGroupPref:
                result = Path.Combine(GetBaseAppPath(), "preferences", "tagreportgroups.json");
                break;
            case AppPath.tagCounterPref:
                result = Path.Combine(GetBaseAppPath(), "preferences", "tagcounters.json");
                break;

        }

        return result;
    }

    static private string GetBaseAppPath()
    {
        if (!String.IsNullOrEmpty(_basePath))
            return _basePath;

        _basePath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
        return _basePath;
    }



    /// <summary>
    /// Attempt to map application type tags or file type or language to identify
    /// WebApplications, Windows Services, Client Apps, WebServices, Azure Functions etc.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="language"></param>
    /// <param name="tag"></param>
    /// <param name="sample"></param>
    static public String DetectSolutionType(string fileName, string language, string tag, string sample)
    {
        string result = "";

        if (tag.Contains("Application.Type"))
        {
            int lastDot = sample.LastIndexOf(".");
            if (-1 != lastDot)
                result = sample.Substring(lastDot + 1);
        }
        else if (tag.Contains("Microsoft.MVC"))
        {
            result = "webapplication";
        }
        else if (tag.Contains("Microsoft.MFC") || tag.Contains(".WinSDK"))
        {
            result = "winclient";
        }
        else
        {
            ///////////first chance
            switch (fileName)
            {
                case "web.config":
                    result = "webapplication";
                    break;
                case "app.config":
                    result = ".netclient";
                    break;
                case "pom.xml":
                case "build.make.xml":
                case "build.gradle":
                    result = "java";
                    break;
                case "package.json":
                    result = "node";
                    break;
            }

            if (string.IsNullOrEmpty(result))
            {
                ////////////second chance
                switch (Path.GetExtension(fileName))
                {
                    case ".cshtml":
                        result = "ASP.NET";
                        break;
                    case ".htm":
                    case ".html":
                    case ".js":
                        result = "webapplication";
                        break;
                    case "powershell":
                    case "shellscript":
                    case "wincmdscript":
                        result = "script";
                        break;
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                ///////////third chance
                switch (language)
                {
                    case "ruby":
                    case "perl":
                    case "php":
                        result = "webapplication";
                        break;
                }
            }

        }

        return result;

    }



    public static void OpenBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROWSER")))
            {
                try
                {
                    Process.Start("xdg-open", url);
                }
                catch (Exception)
                {
                    WriteOnce.Error("Unable to open browser.  Open output file directly.");
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
    }


    public static string GetResourceString(ResourceMsg.ID id)
    {
        string result="";
        try
        {
            result = Resources.ResourceManager.GetString(id.ToString());
        }
        catch (Exception e)
        {
            string error = string.Format("Unable to locate requested string resource {0}", id);
            error += e.Message + "\n" + e.StackTrace;
            throw new Exception(error);
        }

        return result;

    }

    public static string FormatResourceString(ResourceMsg.ID id, params object[] parameters)
    {
        return String.Format(GetResourceString(id), parameters);
    }

    public static string FormatResourceString(ResourceMsg.ID id, int value)
    {
        return String.Format(GetResourceString(id), value);
    }
}
