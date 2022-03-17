// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.Common
{
    using CommandLine;

    /// <summary>
    /// base for common options across all commands
    /// </summary>
    public class LogOptions
    {
        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [Verbose|Debug|Information|Warning|Error|Fatal|Off]", Default = "Information")]
        public string ConsoleVerbosityLevel { get; set; } = "medium";

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string? LogFilePath { get; set; }

        [Option('v', "log-file-level", Required = false, HelpText = "Log file level [Verbose|Debug|Information|Warning|Error|Fatal|Off]", Default = "Error")]
        public string LogFileLevel { get; set; } = "Error";
    }
}