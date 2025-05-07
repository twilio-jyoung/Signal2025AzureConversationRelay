using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

public sealed class ColorConsoleLoggerConfiguration
{
    public int EventId { get; set; }

    public Dictionary<LogLevel, ConsoleColor> LogLevelToColorMap { get; set; } = new()
    {
        [LogLevel.Trace] = ConsoleColor.Gray,
        [LogLevel.Debug] = ConsoleColor.Cyan,
        [LogLevel.Information] = ConsoleColor.Green,
        [LogLevel.Warning] = ConsoleColor.Yellow,
        [LogLevel.Error] = ConsoleColor.Red,
        [LogLevel.Critical] = ConsoleColor.Magenta
    };

    public Dictionary<LogLevel, string> LogLevelToANSIColorMap { get; set; } = new()
    {
        [LogLevel.Trace] = "\u001b[90m",
        [LogLevel.Debug] = "\u001b[92m",
        [LogLevel.Information] = "\u001b[36m",
        [LogLevel.Warning] = "\u001b[33m",
        [LogLevel.Error] = "\u001b[31m",
        [LogLevel.Critical] = "\u001b[35m"
    };
}