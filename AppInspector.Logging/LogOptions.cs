// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using CommandLine;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Microsoft.ApplicationInspector.Logging;

/// <summary>
///     base for common options for the logging in commands
/// </summary>
public record LogOptions
{
    [Option('x', "console-verbosity", Required = false,
        HelpText = "Console verbosity [Verbose|Debug|Information|Warning|Error|Fatal]",
        Default = LogEventLevel.Information)]
    public LogEventLevel ConsoleVerbosityLevel { get; set; } = LogEventLevel.Information;

    [Option("disable-console", Required = false, HelpText = "Disable console output of logging messages.",
        Default = false)]
    public bool DisableConsoleOutput { get; set; } = false;

    [Option('v', "log-file-level", Required = false,
        HelpText = "Log file level [Verbose|Debug|Information|Warning|Error|Fatal]", Default = LogEventLevel.Error)]
    public LogEventLevel LogFileLevel { get; set; } = LogEventLevel.Error;

    [Option('l', "log-file-path", Required = false, HelpText = "Log file path. If not set, will not log to file.")]
    public string? LogFilePath { get; set; }

    public ILoggerFactory GetLoggerFactory()
    {
        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(ConsoleVerbosityLevel < LogFileLevel ? ConsoleVerbosityLevel : LogFileLevel);
        if (!string.IsNullOrEmpty(LogFilePath)) configuration = configuration.WriteTo.File(LogFilePath, LogFileLevel);
        if (!DisableConsoleOutput) configuration = configuration.WriteTo.Console(ConsoleVerbosityLevel);
        var serilogLogger = configuration
            .CreateLogger();
        return new LoggerFactory().AddSerilog(serilogLogger);
    }
}