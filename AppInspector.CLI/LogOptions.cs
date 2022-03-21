﻿// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.CLI
{
    using CommandLine;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using System;

    /// <summary>
    /// base for common options for the CLI.
    /// </summary>
    public class LogOptions
    {
        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [Verbose|Debug|Information|Warning|Error|Fatal]", Default = Serilog.Events.LogEventLevel.Information)]
        public Serilog.Events.LogEventLevel ConsoleVerbosityLevel { get; set; } = Serilog.Events.LogEventLevel.Information;

        [Option("disable-console", Required = false, HelpText = "Disable console output of logging messages.", Default = false)]
        public bool DisableConsoleOutput { get; set; } = false;

        [Option('v', "log-file-level", Required = false, HelpText = "Log file level [Verbose|Debug|Information|Warning|Error|Fatal]", Default = Serilog.Events.LogEventLevel.Error)]
        public Serilog.Events.LogEventLevel LogFileLevel { get; set; } = Serilog.Events.LogEventLevel.Error;

        [Option("disable-log-file", Required = false, HelpText = "Disable file output of logging messages.", Default = false)]
        public bool DisableLogFileOutput { get; set; } = false;

        [Option('l', "log-file-path", Required = false, HelpText = $"Log file path. If not set, will not log to file.")]
        public string? LogFilePath { get; set; }

        public ILoggerFactory GetLoggerFactory()
        {
            var configuration = new LoggerConfiguration()
                .MinimumLevel.Is(ConsoleVerbosityLevel < LogFileLevel ? ConsoleVerbosityLevel : LogFileLevel);
            if (!DisableLogFileOutput && !string.IsNullOrEmpty(LogFilePath))
            {
                configuration.WriteTo.File(LogFilePath, LogFileLevel);
            }
            if (!DisableConsoleOutput)
            {
                configuration.WriteTo.Console(ConsoleVerbosityLevel);
            }
            var serilogger = configuration
                .CreateLogger();
            return new LoggerFactory().AddSerilog(serilogger);
        }
    }
}