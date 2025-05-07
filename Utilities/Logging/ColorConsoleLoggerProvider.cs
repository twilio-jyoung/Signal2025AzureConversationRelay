using System;
using Microsoft.Extensions.Logging;

public class ColorConsoleLoggerProvider : ILoggerProvider
{
    private readonly Func<ColorConsoleLoggerConfiguration> _getCurrentConfig;

    public ColorConsoleLoggerProvider(Func<ColorConsoleLoggerConfiguration> getCurrentConfig)
    {
        _getCurrentConfig = getCurrentConfig;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ColorConsoleLogger(categoryName, _getCurrentConfig);
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}