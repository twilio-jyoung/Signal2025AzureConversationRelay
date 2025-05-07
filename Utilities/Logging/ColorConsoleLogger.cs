#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;

public sealed class ColorConsoleLogger(
    string name,
    Func<ColorConsoleLoggerConfiguration> getCurrentConfig) : ILogger
{
    private string? callSid = null;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // we will only use scopes to scope logs to the current call sid
        callSid = state.ToString();

        return new Scope(() =>
        {
            callSid = null;
        });
    }

    public bool IsEnabled(LogLevel logLevel) =>
        getCurrentConfig().LogLevelToColorMap.ContainsKey(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        ColorConsoleLoggerConfiguration config = getCurrentConfig();
        if (config.EventId == 0 || config.EventId == eventId.Id)
        {
            var white = "\u001b[97m";
            var red = "\u001b[31m";

            if (callSid != null)
            {
                var scopeInfo = $"[{callSid}] ";
                Console.Write($"{red}{scopeInfo}");
            }

            var logLevelColor = config.LogLevelToANSIColorMap[logLevel];
            Console.WriteLine($"{white}[{name.Split(".")[^1]}] {logLevelColor}{formatter(state, exception)}");  //{red}{scopeInfo}
        }
    }

    private class Scope : IDisposable
    {
        private readonly Action _onDispose;
        private bool _disposed;

        public Scope(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _onDispose();
                _disposed = true;
            }
        }
    }
}

