using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.Common;

public static class Utils
{
    /// <summary>
    ///     Converts a strings to a compiled regex.
    ///     Uses an internal cache.
    /// </summary>
    /// <param name="built">The regex to build</param>
    /// <param name="regexOptions">The options to use.</param>
    /// <returns>The built Regex</returns>
    public static Regex? StringToRegex(string built, RegexOptions regexOptions)
    {
        try
        {
            return new Regex(built, regexOptions);
        }
        catch (NotSupportedException)
        {
#if NET7_0_OR_GREATER
            // Its possible that this regex is not compatible with the non-backtracking engine
            // Try constructing it without NonBackTracking
            regexOptions &= ~RegexOptions.NonBacktracking;
            try
            {
                return new Regex(built, regexOptions);
            }
            catch (Exception)
            {
            }
#endif
        }
        catch (Exception)
        {
        }

        return null;
    }

    /// <summary>
    ///     Converts a strings to a compiled regex.
    ///     Uses an internal cache.
    /// </summary>
    /// <param name="built">The regex to build</param>
    /// <param name="modifiers">The options to use.</param>
    /// <returns>The built Regex</returns>
    public static Regex? StringToRegex(string built, IList<string> modifiers) => StringToRegex(built, RegexModifierToRegexOptions(modifiers));

    /// <summary>
    /// Convert a list of string modifiers specified in a SearchPattern to the appropriate regex modifiers
    /// </summary>
    /// <param name="modifiers"></param>
    /// <returns>A RegexOptions object with the correct modifiers set</returns>
    public static RegexOptions RegexModifierToRegexOptions(IList<string> modifiers)
    {
        RegexOptions opts = new();
        opts |= RegexOptions.Compiled;

        foreach (var modifier in modifiers)
        {
            switch (modifier.ToLower())
            {
                case "m":
                case "multiline":
                    opts |= RegexOptions.Multiline;
                    break;
                case "s":
                case "singleline":
                    opts |= RegexOptions.Singleline;
                    break;
                case "i":
                case "ignorecase":
                    opts |= RegexOptions.IgnoreCase;
                    break;
                case "c":
                case "cultureinvariant":
                    opts |= RegexOptions.CultureInvariant;
                    break;
                // This is 'n' to match the options for the Regex api: https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options
                case "n":
                case "explicitcapture":
                    opts |= RegexOptions.ExplicitCapture;
                    break;
                case "x":
                case "ignorepatternwhitespace":
                    opts |= RegexOptions.IgnorePatternWhitespace;
                    break;
                case "e":
                case "ecmascript":
                    opts |= RegexOptions.ECMAScript;
                    // ECMAScript option requires multiline, ignore case and compiled per docs
                    opts |= RegexOptions.Multiline;
                    opts |= RegexOptions.IgnoreCase;
                    break;
                case "r":
                case "righttoleft":
                    opts |= RegexOptions.RightToLeft;
                    break;
                case "b":
#if NET7_0_OR_GREATER

                opts &= ~RegexOptions.NonBacktracking;
#endif
                    break;
                case "nb":
#if NET7_0_OR_GREATER

                opts |= RegexOptions.NonBacktracking;
#endif
                    break;
                default:
                    break;
            }
        }

        return opts;
    }
    public enum AppPath
    {
        basePath,
        defaultRulesSrc,
        defaultRulesPackedFile,
        defaultLog,
        tagGroupPref,
        tagCounterPref
    }

    public enum ExitCode
    {
        Success = 0,
        PartialFail = 1,
        CriticalError = 2
    }

    private static string? _basePath;

    public static bool CLIExecutionContext { get; set; }

    public static string GetVersionString()
    {
        return string.Format("Microsoft Application Inspector {0}", GetVersion());
    }

    public static string GetVersion()
    {
        return (Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false) as
            AssemblyInformationalVersionAttribute[])?[0].InformationalVersion ?? "Unknown";
    }

    public static string GetPath(AppPath pathType)
    {
        var result = "";
        switch (pathType)
        {
            case AppPath.basePath:
                result = GetBaseAppPath();
                break;

            case AppPath.defaultLog:
                result = "appinspector.log.txt";
                break;

            case AppPath.defaultRulesSrc: //Packrules source use
                result = Path.GetFullPath(Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector",
                    "rules", "default")); //used to ref project folder
                break;

            case AppPath.defaultRulesPackedFile: //Packrules default output use
                result = Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector", "Resources",
                    "defaultRulesPkd.json"); //packed default file in project resources
                break;

            case AppPath.tagGroupPref: //CLI use only
                result = Path.Combine(GetBaseAppPath(), "preferences", "tagreportgroups.json");
                break;

            case AppPath.tagCounterPref: //CLI use only
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

        _basePath = Path.GetFullPath(AppContext.BaseDirectory);
        return _basePath;
    }
}