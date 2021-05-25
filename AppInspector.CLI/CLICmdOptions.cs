// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using CommandLine;
using Microsoft.ApplicationInspector.Commands;
using System.Collections.Generic;

namespace Microsoft.ApplicationInspector.CLI
{
    /// <summary>
    /// CLI command option classes add output arguments to common properties for each command verb
    /// </summary>
    ///
    public class CLICommandOptions : CommandOptions
    {
        [Option('o', "output-file-path", Required = false, HelpText = "Output file path")]
        public string? OutputFilePath { get; set; }

        [Option('f', "output-file-format", Required = false, HelpText = "Output format [json|text]", Default = "text")]
        public string OutputFileFormat { get; set; } = "text";
    }

    /// <summary>
    /// CLI command distinct arguments
    /// </summary>
    [Verb("analyze", HelpText = "Inspect source directory/file/compressed file (.tgz|zip) against defined characteristics")]
    public class CLIAnalyzeCmdOptions : CLICommandOptions
    {
        [Option('s', "source-path", Required = true, HelpText = "Source file or directory to inspect, comma separated", Separator = ',')]
        public IEnumerable<string> SourcePath { get; set; } = new string[0];

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules file or directory path")]
        public string? CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('c', "confidence-filters", Required = false, HelpText = "Output only matches with specified confidence <value>,<value> [high|medium|low]", Default = "high,medium")]
        public string ConfidenceFilters { get; set; } = "high,medium";

        [Option('k', "file-path-exclusions", Required = false, HelpText = "Exclude source files that match glob patterns. Example: \".git,**Tests**\".  Use \"none\" to disable.", Default = new string[] { "**/bin/**", "**/obj/**", "**/.vs/**", "**/.git/**" }, Separator = ',')]

        public IEnumerable<string> FilePathExclusions { get; set; } = new string[0];

        [Option('f', "output-file-format", Required = false, HelpText = "Output format [html|json|text]", Default = "html")]
        public new string OutputFileFormat { get; set; } = "html";

        [Option('e', "text-format", Required = false, HelpText = "Match text format specifiers", Default = "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Sourcetype:%t,Line:%L,Sample:%m")]
        public string TextOutputFormat { get; set; } = "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Sourcetype:%t,Line:%L,Sample:%m";

        [Option("file-timeout", Required = false, HelpText = "If set, maximum amount of time in milliseconds to allow for processing each file.", Default = 60000)]
        public int FileTimeOut { get; set; } = 60000;

        [Option("processing-timeout", Required = false, HelpText = "If set, maximum amount of time in milliseconds to allow for processing overall.", Default = 0)]
        public int ProcessingTimeOut { get; set; } = 0;

        [Option("single-threaded", Required = false, HelpText = "Disables parallel processing.")]
        public bool SingleThread { get; set; }

        [Option("no-show-progress", Required = false, HelpText = "Disable progress information.")]
        public bool NoShowProgressBar { get; set; }

        [Option("context-lines", Required = false, HelpText = "Number of lines of context on each side to include in excerpt. -1 to not extract samples or excerpts.")]
        public int ContextLines { get; set; } = 3;

        [Option("scan-unknown-filetypes", Required = false, HelpText = "Scan files of unknown types.")]
        public bool ScanUnknownTypes { get; set; }

        [Option('t',"tags-only", Required = false, HelpText = "Only get tags (no detailed match data).")]
        public bool TagsOnly { get; set; }
    }

    [Verb("tagdiff", HelpText = "Compares unique tag values between two source paths")]
    public class CLITagDiffCmdOptions : CLICommandOptions
    {
        [Option("src1", Required = true, HelpText = "Source 1 to compare (commaa separated)")]
        public IEnumerable<string> SourcePath1 { get; set; } = System.Array.Empty<string>();

        [Option("src2", Required = true, HelpText = "Source 2 to compare (commaa separated)")]
        public IEnumerable<string> SourcePath2 { get; set; } = System.Array.Empty<string>();

        [Option('t', "test-type", Required = false, HelpText = "Type of test to run [equality|inequality]", Default = "equality")]
        public string TestType { get; set; } = "equality";

        [Option('k', "file-path-exclusions", Required = false, HelpText = "Exclude source files that match glob patterns. Example: \".git,**Tests**\".  Use \"none\" to disable.", Default = "*/bin,*/obj,*/.vs,*/.git", Separator = ',')]
        public IEnumerable<string> FilePathExclusions { get; set; } = new string[] { };

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules file or directory path")]
        public string? CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option("file-timeout", Required = false, HelpText = "If set, maximum amount of time in milliseconds to allow for processing each file.", Default = 60000)]
        public int FileTimeOut { get; set; } = 60000;

        [Option("processing-timeout", Required = false, HelpText = "If set, maximum amount of time in milliseconds to allow for processing overall.", Default = 0)]
        public int ProcessingTimeOut { get; set; } = 0;
    }

    [Verb("exporttags", HelpText = "Export unique rule tags to view what code features may be detected")]
    public class CLIExportTagsCmdOptions : CLICommandOptions
    {
        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules file or directory path")]
        public string? CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }
    }

    [Verb("verifyrules", HelpText = "Verify custom rules syntax is valid")]
    public class CLIVerifyRulesCmdOptions : CLICommandOptions
    {
        [Option('d', "verify-default-rules", Required = false, HelpText = "Verify default rules")]
        public bool VerifyDefaultRules { get; set; }

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules file or directory path")]
        public string? CustomRulesPath { get; set; }

        [Option('a', "fail-fast", Required = false, HelpText = "Fail fast", Default = false)]
        public bool Failfast { get; set; }
    }

    [Verb("packrules", HelpText = "Combine multiple rule files into one file for ease in distribution")]
    public class CLIPackRulesCmdOptions : CLICommandOptions
    {
        [Option('d', "pack-default-rules", Required = false, HelpText = "Repack default rules. Automatic on Application Inspector build.  Not intended for use outside of build.")]
        public bool RepackDefaultRules { get; set; }

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules file or directory path")]
        public string? CustomRulesPath { get; set; }

        [Option('f', "output-file-format", Required = false, HelpText = "Output format [json]", Default = "json")]
        public new string OutputFileFormat { get; set; } = "json";

        [Option('i', "not-indented", Required = false, HelpText = "Remove indentation from json output", Default = false)]
        public bool NotIndented { get; set; }
    }
}