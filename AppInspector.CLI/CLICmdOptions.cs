// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using CommandLine;
using Microsoft.ApplicationInspector.Commands;

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
        [Option('s', "source-path", Required = true, HelpText = "Source file or directory to inspect")]
        public string? SourcePath { get; set; }

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules file or directory path")]
        public string? CustomRulesPath { get; set; }

        [Option('h', "match-depth", Required = false, HelpText = "First match or best match based on confidence level (first|best)", Default = "best")]
        public string MatchDepth { get; set; } = "best";

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('d', "allow-dup-tags", Required = false, HelpText = "Output contains unique and non-unique tag matches", Default = false)]
        public bool AllowDupTags { get; set; }

        [Option('b', "suppress-browser-open", Required = false, HelpText = "Suppress automatically opening HTML output using default browser", Default = false)]
        public bool SuppressBrowserOpen { get; set; }

        [Option('c', "confidence-filters", Required = false, HelpText = "Output only matches with specified confidence <value>,<value> [high|medium|low]", Default = "high,medium")]
        public string ConfidenceFilters { get; set; } = "high,medium";

        [Option('k', "file-path-exclusions", Required = false, HelpText = "Exclude source files (none|default: sample,example,test,docs,lib,.vs,.git)", Default = "sample,example,test,docs,lib,.vs,.git")]
        public string FilePathExclusions { get; set; } = "sample,example,test,docs,.vs,.git";

        [Option('f', "output-file-format", Required = false, HelpText = "Output format [html|json|text]", Default = "html")]
        public new string OutputFileFormat { get; set; } = "html";

        [Option('e', "text-format", Required = false, HelpText = "Match text format specifiers", Default = "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Sourcetype:%t,Line:%L,Sample:%m")]
        public string TextOutputFormat { get; set; } = "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Sourcetype:%t,Line:%L,Sample:%m";

        [Option('t', "tag-output-only", Required = false, HelpText = "Output only identified tags", Default = false)]
        public bool SimpleTagsOnly { get; set; }

        [Option("single-threaded", Required = false, HelpText = "Disables parallel processing.")]
        public bool SingleThread { get; set; }
    }

    [Verb("tagdiff", HelpText = "Compares unique tag values between two source paths")]
    public class CLITagDiffCmdOptions : CLICommandOptions
    {
        [Option("src1", Required = true, HelpText = "Source 1 to compare")]
        public string? SourcePath1 { get; set; }

        [Option("src2", Required = true, HelpText = "Source 2 to compare")]
        public string? SourcePath2 { get; set; }

        [Option('t', "test-type", Required = false, HelpText = "Type of test to run [equality|inequality]", Default = "equality")]
        public string TestType { get; set; } = "equality";

        [Option('k', "file-path-exclusions", Required = false, HelpText = "Exclude source files (none|default: sample,example,test,docs,.vs,.git)", Default = "sample,example,test,docs,.vs,.git")]
        public string FilePathExclusions { get; set; } = "sample,example,test,docs,.vs,.git";

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules file or directory path")]
        public string? CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }
    }

    [Verb("tagtest", HelpText = "Test (T/F) for presence of custom rule set in source")]
    public class CLITagTestCmdOptions : CLICommandOptions
    {
        [Option('s', "source-path", Required = true, HelpText = "Source file or directory to inspect")]
        public string? SourcePath { get; set; }

        [Option('t', "test-type", Required = false, HelpText = "Test to perform [rulespresent|rulesnotpresent] ", Default = "rulespresent")]
        public string TestType { get; set; } = "rulespresent";

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules file or directory path")]
        public string? CustomRulesPath { get; set; }

        [Option('k', "file-path-exclusions", Required = false, HelpText = "Exclude source files (none|default: sample,example,test,docs,.vs,.git)", Default = "sample,example,test,docs,.vs,.git")]
        public string FilePathExclusions { get; set; } = "sample,example,test,docs,.vs,.git";
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