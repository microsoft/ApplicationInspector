using Microsoft.Extensions.Logging;
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
    ///     Converts a strings to a compiled regex. Returns null if the Regex cannot be constructed with the given arguments. See <paramref name="logger"/> for details.
    /// </summary>
    /// <param name="built">The regex to build</param>
    /// <param name="regexOptions">The options to use.</param>
    /// <param name="logger">Optional logger to get any exception details details</param>
    /// <returns>The built Regex, or null if a Regex could not be constructed</returns>
    public static Regex? StringToRegex(string built, RegexOptions regexOptions, ILogger? logger)
    {
        try
        {
            return new Regex(built, regexOptions);
        }
#if NET7_0_OR_GREATER
        catch (NotSupportedException)
        {
            // Its possible that this regex is not compatible with the non-backtracking engine
            // Try constructing it without NonBackTracking
            regexOptions &= ~RegexOptions.NonBacktracking;
            try
            {
                Regex backTrackedRegex = new Regex(built, regexOptions);
                logger?.LogDebug("Could not construct the regular expression {pattern} with NonBackTracking so it was constructed without NonBackTracking.", built);
                return backTrackedRegex;
            }
            catch (Exception e)
            {
                logger?.LogWarning("Could not construct the regular expression {pattern} with options {regOpts}. ({exceptionType}: {exceptionMessage})", built, string.Join(",", regexOptions), e.GetType().Name, e.Message);
            }
        }
#endif
        catch (Exception e)
        {
            logger?.LogWarning("Could not construct the regular expression {pattern} with options {regOpts}. ({exceptionType}: {exceptionMessage})", built, string.Join(",", regexOptions), e.GetType().Name, e.Message);
        }

        return null;
    }

    /// <summary>
    ///     Converts a strings to a compiled regex. Returns null if the Regex cannot be constructed with the given arguments. See <paramref name="logger"/> for details.
    /// </summary>
    /// <param name="built">The regex to build</param>
    /// <param name="modifiers">The options to use.</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>The built Regex or null if the provided arguments cannot construct a Regex.</returns>
    public static Regex? StringToRegex(string built, IList<string> modifiers, ILogger? logger) => StringToRegex(built, RegexModifierToRegexOptions(modifiers), logger);

    /// <summary>
    ///     Convert a list of string modifiers specified in a SearchPattern to the appropriate <see cref="RegexOptions"/>
    /// </summary>
    /// <param name="modifiers">A list of specified Regex options</param>
    /// <returns>A RegexOptions object with the correct modifiers set</returns>
    public static RegexOptions RegexModifierToRegexOptions(IList<string> modifiers)
    {
        RegexOptions opts = new();
        opts |= RegexOptions.Compiled;

        foreach (string modifier in modifiers)
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
                    // The NonBackTracking option was added in .NET 7
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