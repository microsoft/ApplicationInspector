// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using CommandLine;
using NLog;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// base for common options across all commands
    /// </summary>
    public class CommandOptions
    {
        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [high|medium|low|none]", Default = "medium")]
        public string ConsoleVerbosityLevel { get; set; }
        public Logger Log { get; set; }

        [Option('l', "log-file-path", Required = false, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
        [Option('v', "log-file-level", Required = false, HelpText = "Log file level [Debug|Info|Warn|Error|Trace|Fatal|Off]", Default = "Error")]
        public string LogFileLevel { get; set; }


        public CommandOptions()
        {
            LogFileLevel = "Error";
            ConsoleVerbosityLevel = "medium";
        }

        public bool CloseLogOnCommandExit { get; set; }

    }
}
