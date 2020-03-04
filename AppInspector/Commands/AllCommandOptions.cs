// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using CommandLine;
using NLog;

namespace Microsoft.ApplicationInspector.Commands
{
    public class AllCommandOptions
    {
        public Logger Log { get; set; }

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level [Debug|Info|Warn|Error|Fatal|Off]", Default = "Error")]
        public string LogFileLevel { get; set; }

    }
    /// <summary>
    /// Command option classes for each command verb
    /// </summary>

    [Verb("analyze", HelpText = "Inspect source directory/file/compressed file (.tgz|zip) against defined characteristics")]
    public class AnalyzeCommandOptions : AllCommandOptions
    {
        [Option('s', "source-path", Required = true, HelpText = "Path to source code to inspect (required)")]
        public string SourcePath { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('f', "output-file-format", Required = false, HelpText = "Output format [html|json|text]", Default = "html")]
        public string OutputFileFormat { get; set; }

        [Option('e', "text-format", Required = false, HelpText = "Match text format specifiers", Default = "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Sourcetype:%t,Line:%L,Sample:%m")]
        public string TextOutputFormat { get; set; }

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('t', "tag-output-only", Required = false, HelpText = "Output only identified tags", Default = false)]
        public bool SimpleTagsOnly { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('d', "allow-dup-tags", Required = false, HelpText = "Output contains unique and non-unique tag matches", Default = false)]
        public bool AllowDupTags { get; set; }

        [Option('b', "supress-browser-open", Required = false, HelpText = "HTML formatted output is automatically opened to default browser", Default = false)]
        public bool AutoBrowserOpen { get; set; }

        [Option('c', "confidence-filters", Required = false, HelpText = "Output only matches with specified confidence <value>,<value> [high|medium|low]", Default = "high,medium")]
        public string ConfidenceFilters { get; set; }

        [Option('k', "file-path-exclusions", Required = false, HelpText = "Exclude source files (none|default: sample,example,test,docs,.vs,.git)", Default = "sample,example,test,docs,.vs,.git")]
        public string FilePathExclusions { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }

    }


    [Verb("tagdiff", HelpText = "Compares unique tag values between two source paths")]
    public class TagDiffCommandOptions : AllCommandOptions
    {
        [Option("src1", Required = true, HelpText = "Source 1 to compare (required)")]
        public string SourcePath1 { get; set; }

        [Option("src2", Required = true, HelpText = "Source 2 to compare (required")]
        public string SourcePath2 { get; set; }

        [Option('t', "test-type", Required = false, HelpText = "Type of test to run [equality|inequality]", Default = "equality")]
        public string TestType { get; set; }

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }

    }

    [Verb("tagtest", HelpText = "Test presence of smaller set or custom tags in source (compare or verify modes)")]
    public class TagTestCommandOptions : AllCommandOptions
    {
        [Option('s', "source-path", Required = true, HelpText = "Source to test (required)")]
        public string SourcePath { get; set; }

        [Option('t', "test-type", Required = false, HelpText = "Test to perform [rulespresent|rulesnotpresent] ", Default = "rulespresent")]
        public string TestType { get; set; }

        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = true)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }

    }

    [Verb("exporttags", HelpText = "Export default unique rule tags to view what features may be detected")]
    public class ExportTagsCommandOptions : AllCommandOptions
    {
        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }
    }


    [Verb("verifyrules", HelpText = "Verify rules syntax is valid")]
    public class VerifyRulesCommandOptions : AllCommandOptions
    {
        [Option('r', "custom-rules-path", Required = false, HelpText = "Custom rules path")]
        public string CustomRulesPath { get; set; }

        [Option('i', "ignore-default-rules", Required = false, HelpText = "Exclude default rules bundled with application", Default = false)]
        public bool IgnoreDefaultRules { get; set; }

        [Option('o', "output-file-path", Required = false, HelpText = "Path to output file")]
        public string OutputFilePath { get; set; }

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }


    }


}
