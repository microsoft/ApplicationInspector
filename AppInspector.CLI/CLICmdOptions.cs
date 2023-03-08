// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using CommandLine;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;

namespace Microsoft.ApplicationInspector.CLI;

/// <summary>
///     CLI command option classes add output arguments to common properties for each command verb
/// </summary>
public record CLICommandOptions : LogOptions
{
    [Option('o', "output-file-path", Required = false, HelpText = "Output file path")]
    public string? OutputFilePath { get; set; }

    [Option('f', "output-file-format", Required = false, HelpText = "Output format [json|text]", Default = "text")]
    public string OutputFileFormat { get; set; } = "text";
}

public record CLICustomRulesCommandOptions : CLICommandOptions
{
    [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules file or directory path")]
    public string? CustomRulesPath { get; set; }

    [Option("custom-languages-path", Required = false,
        HelpText = "Replace the default languages set with a custom languages.json.")]
    public string? CustomLanguagesPath { get; set; }

    [Option("custom-comments-path", Required = false,
        HelpText = "Replace the default comment specification set with a custom comments.json.")]
    public string? CustomCommentsPath { get; set; }

    [Option("disable-require-unique-ids", Required = false, HelpText = "Allow rules with duplicate IDs.")]
    public bool DisableRequireUniqueIds { get; set; }

    /// <summary>
    ///     Return a success error code when no matches were found but operation was apparently successful. Useful for CI
    ///     scenarios
    /// </summary>
    [Option("success-error-code-with-no-matches", Required = false,
        HelpText =
            "When processing is apparently successful but there are no matches return a success error code - useful for CI.")]
    public bool SuccessErrorCodeOnNoMatches { get; set; }

    [Option("require-must-match", Required = false,
        HelpText = "When validating, require rules to have MustMatch self-tests.")]
    public bool RequireMustMatch { get; set; }

    [Option("require-must-not-match", Required = false,
        HelpText = "When validating, require rules to have MustNotMatch self-tests.")]
    public bool RequireMustNotMatch { get; set; }

    [Option('R', "non-backtracking-regex", Required = false,
    HelpText = "Prefer non-backtracking regex for all rules unless they require the backtracking engine. A warning will be displayed for all regular expressions that require backtracking support. Default: Off.",
    Default = false),]
    public bool EnableNonBacktrackingRegex { get; set; }
}

public record CLIAnalysisSharedCommandOptions : CLICustomRulesCommandOptions
{
    [Option("disable-custom-rule-validation", Required = false,
        HelpText = "By default when providing custom rules they are validated. When set, validation will be skipped.")]
    public bool DisableCustomRuleValidation { get; set; } = false;

    [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application",
        Default = false)]
    public bool IgnoreDefaultRules { get; set; }

    [Option('F', "file-timeout", Required = false,
        HelpText =
            "Maximum amount of time in milliseconds to allow for processing each file. 0 is infinity. Default: 60000.",
        Default = 60000)]
    public int FileTimeOut { get; set; } = 60000;

    [Option('p', "processing-timeout", Required = false,
        HelpText =
            "Maximum amount of time in milliseconds to allow for processing. When NoShowProgress is set this includes enumeration time. 0 is infinity. Default: 0.",
        Default = 0)]
    public int ProcessingTimeOut { get; set; }

    [Option("enumeration-timeout", Required = false,
        HelpText = "Maximum amount of time in milliseconds to allow for enumerating. 0 is infinity. Default: 0.",
        Default = 0)]
    public int EnumeratingTimeout { get; set; }

    [Option("disable-archive-crawling", Required = false, HelpText = "Disable Archive Enumeration.")]
    public bool DisableArchiveCrawling { get; set; }

    [Option('S', "single-threaded", Required = false,
        HelpText = "Disables parallel processing. May be helpful for debugging with higher verbosity.")]
    public bool SingleThread { get; set; }

    [Option('g', "exclusion-globs", Required = false,
        HelpText =
            "Exclude source files that match glob patterns. Example: \"**/.git/**,*Tests*\".  Use \"none\" to disable.",
        Default = new[] { "**/bin/**", "**/obj/**", "**/.vs/**", "**/.git/**" }, Separator = ',')]
    public IEnumerable<string> FilePathExclusions { get; set; } = Array.Empty<string>();

    [Option('u', "scan-unknown-filetypes", Required = false, HelpText = "Scan files of unknown types.")]
    public bool ScanUnknownTypes { get; set; }

    [Option('c', "confidence-filters", Required = false, Separator = ',',
        HelpText =
            "Output only matches with specified confidence <value>,<value>. Default: Medium,High. [High|Medium|Low]",
        Default = new[] { Confidence.High, Confidence.Medium })]
    public IEnumerable<Confidence> ConfidenceFilters { get; set; } = new[] { Confidence.High, Confidence.Medium };

    [Option("severity-filters", Required = false, Separator = ',',
        HelpText =
            "Output only matches with specified severity <value>,<value>. Default: All are enabled. [Critical|Important|Moderate|BestPractice|ManualReview]",
        Default = new[]
            { Severity.Critical, Severity.Important, Severity.Moderate, Severity.BestPractice, Severity.ManualReview })]
    public IEnumerable<Severity> SeverityFilters { get; set; } = new[]
        { Severity.Critical, Severity.Important, Severity.Moderate, Severity.BestPractice, Severity.ManualReview };

}

/// <summary>
///     CLI command distinct arguments
/// </summary>
[Verb("analyze", HelpText = "Inspect source directory/file/compressed file (.tgz|zip) against defined characteristics")]
public record CLIAnalyzeCmdOptions : CLIAnalysisSharedCommandOptions
{
    [Option('s', "source-path", Required = true, HelpText = "Source file or directory to inspect, comma separated",
        Separator = ',')]
    public IEnumerable<string> SourcePath { get; set; } = Array.Empty<string>();

    [Option('f', "output-file-format", Required = false, HelpText = "Output format [html|json|text]", Default = "html")]
    public new string OutputFileFormat { get; set; } = "html";

    [Option('e', "text-format", Required = false, HelpText = "Match text format specifiers",
        Default = "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Sourcetype:%t,Line:%L,Sample:%m")]
    public string TextOutputFormat { get; set; } =
        "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Sourcetype:%t,Line:%L,Sample:%m";

    [Option('N', "no-show-progress", Required = false, HelpText = "Disable progress information.")]
    public bool NoShowProgressBar { get; set; }

    [Option('C', "context-lines", Required = false,
        HelpText =
            "Number of lines of context on each side to include in excerpt (up to a maximum of 100 * NumLines characters on each side). 0 to skip exerpt. -1 to not extract samples or excerpts (implied by -t). When outputting sarif use -1 for no snippets, all other values ignored.")]
    public int ContextLines { get; set; } = 3;

    [Option('t', "tags-only", Required = false,
        HelpText = "Only get tags (no detailed match data). Ignored if output format is sarif.")]
    public bool TagsOnly { get; set; }

    [Option('n', "no-file-metadata", Required = false, HelpText = "Don't collect metadata about each individual file.")]
    public bool NoFileMetadata { get; set; }

    [Option('A', "allow-all-tags-in-build-files", Required = false,
        HelpText = "Allow all tags (not just Metadata tags) in files of type Build.")]
    public bool AllowAllTagsInBuildFiles { get; set; }

    [Option('M', "max-num-matches-per-tag", Required = false,
        HelpText =
            "If non-zero, and TagsOnly is not set, will ignore rules based on if all of their tags have been found the set value number of times.")]
    public int MaxNumMatchesPerTag { get; set; } = 0;

    [Option("base-path", Required = false,
        HelpText = "If set, when outputting sarif, will have paths made relative to the provided path.")]
    public string? BasePath { get; set; } = null;

    [Option("repository-uri", Required = false, HelpText = "If set, override any automatically detected RepositoryUri in Sarif report.")]
    public string? RepositoryUri { get; set; } = null;

    [Option("commit-hash", Required = false, HelpText = "If set, override any automatically detected CommitHash in Sarif report.")]
    public string? CommitHash { get; set; } = null;
}

[Verb("tagdiff", HelpText = "Compares unique tag values between two source paths")]
public record CLITagDiffCmdOptions : CLIAnalysisSharedCommandOptions
{
    [Option("src1", Required = true, HelpText = "Source 1 to compare (commaa separated)")]
    public IEnumerable<string> SourcePath1 { get; set; } = Array.Empty<string>();

    [Option("src2", Required = true, HelpText = "Source 2 to compare (commaa separated)")]
    public IEnumerable<string> SourcePath2 { get; set; } = Array.Empty<string>();

    [Option('t', "test-type", Required = false, HelpText = "Type of test to run [Equality|Inequality]",
        Default = TagTestType.Equality)]
    public TagTestType TestType { get; set; } = TagTestType.Equality;
}

[Verb("exporttags",
    HelpText = "Export the list of tags associated with the specified rules. Does not scan source code.")]
public record CLIExportTagsCmdOptions : CLICommandOptions
{
    [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules file or directory path")]
    public string? CustomRulesPath { get; set; }

    [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application",
        Default = false)]
    public bool IgnoreDefaultRules { get; set; }
}

[Verb("verifyrules", HelpText = "Verify custom rules syntax is valid")]
public record CLIVerifyRulesCmdOptions : CLICustomRulesCommandOptions
{
    [Option('d', "verify-default-rules", Required = false, Default = false,
        HelpText = "Verify the rules embedded in the binary.")]
    public bool VerifyDefaultRules { get; set; }
}

[Verb("packrules", HelpText = "Combine multiple rule files into one file for ease in distribution")]
public record CLIPackRulesCmdOptions : CLICustomRulesCommandOptions
{
    [Option('e', "pack-embedded-rules", Required = false,
        HelpText = "Pack the rules that are embedded in the application inspector binary.")]
    public bool PackEmbeddedRules { get; set; }
}