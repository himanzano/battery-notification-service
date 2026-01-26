using System.Collections.Concurrent;
using System.Text;

namespace BatteryNotificationService.Logging;

/// <summary>
/// A custom logger provider that writes log entries to a local file.
/// </summary>
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly BlockingCollection<string> _entryQueue = new();
    private readonly Task _processQueueTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLoggerProvider"/> class.
    /// </summary>
    /// <param name="path">The full path to the log file.</param>
    public FileLoggerProvider(string path)
    {
        _path = path;

        // Ensure directory exists
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _processQueueTask = Task.Run(ProcessQueue);
    }

    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance for the specified category.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>A logger instance.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, this));
    }

    /// <summary>
    /// Queues a log entry to be written to the file.
    /// </summary>
    /// <param name="entry">The formatted log entry string.</param>
    internal void WriteEntry(string entry)
    {
        _entryQueue.Add(entry);
    }

    /// <summary>
    /// Background task that processes the log queue and writes entries to the disk.
    /// </summary>
    private async Task ProcessQueue()
    {
        foreach (var message in _entryQueue.GetConsumingEnumerable())
        {
            try
            {
                await File.AppendAllTextAsync(_path, message + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Ignore logging errors to prevent recursive failures
            }
        }
    }

    /// <summary>
    /// Disposes the provider and ensures all queued logs are processed.
    /// </summary>
    public void Dispose()
    {
        _entryQueue.CompleteAdding();
        try
        {
            _processQueueTask.Wait(1500); // Wait for remaining logs
        }
        catch { }
        _entryQueue.Dispose();
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// A logger implementation that formats and redirects log messages to a <see cref="FileLoggerProvider"/>.
/// </summary>
/// <param name="categoryName">The name of the logger category.</param>
/// <param name="provider">The parent provider handling file operations.</param>
public class FileLogger(string categoryName, FileLoggerProvider provider) : ILogger
{
    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => default!;

    /// <summary>
    /// Checks if the specified log level is enabled.
    /// </summary>
    /// <param name="logLevel">The log level to check.</param>
    /// <returns>True if enabled; otherwise, false.</returns>
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var logEntry =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{categoryName}] {message}";

        if (exception != null)
        {
            logEntry += Environment.NewLine + exception;
        }

        provider.WriteEntry(logEntry);
    }
}
