// Copyright (C) Microsoft. All rights reserved.
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
        private const string DEFAULT_LOG_NAME = "appinspector.log.txt";

        [Option('x', "console-verbosity", Required = false, HelpText = "Console verbosity [Verbose|Debug|Information|Warning|Error|Fatal|Off]", Default = "Information")]
        public string ConsoleVerbosityLevel { get; set; } = "Information";

        [Option('v', "log-file-level", Required = false, HelpText = "Log file level [Verbose|Debug|Information|Warning|Error|Fatal|Off]", Default = "Error")]
        public string LogFileLevel { get; set; } = "Error";

        [Option('l', "log-file-path", Required = false, HelpText = $"Log file path. If not set, will not log to file.")]
        public string? LogFilePath { get; set; }

        public ILoggerFactory GetLoggerFactory(bool noConsole = false)
        {
            var consoleLevel = Enum.TryParse<Serilog.Events.LogEventLevel>(ConsoleVerbosityLevel, out var level) ? level :
#if DEBUG
                Serilog.Events.LogEventLevel.Debug;
#else
                Serilog.Events.LogEventLevel.Information;
#endif
            var fileLogLevel = Enum.TryParse<Serilog.Events.LogEventLevel>(LogFileLevel, out var fileLevel) ? fileLevel : Serilog.Events.LogEventLevel.Error;
            var configuration = new LoggerConfiguration()
                .MinimumLevel.Is(consoleLevel < fileLogLevel ? consoleLevel : fileLogLevel);
            if (!string.IsNullOrEmpty(LogFilePath))
            {
                configuration.WriteTo.File(LogFilePath, fileLogLevel);
            }
            if (!noConsole && !ConsoleVerbosityLevel.Equals("off", StringComparison.InvariantCultureIgnoreCase))
            {
                configuration.WriteTo.Console(consoleLevel);
            }
            var serilogger = configuration
                .CreateLogger();
            return new LoggerFactory().AddSerilog(serilogger);
        }
    }
}