using Microsoft.Extensions.Logging;

namespace RailwayErp.MvvmDemo.Models;

/// <summary>Один запис журналу життєвого циклу.</summary>
public sealed record LogEntry(DateTimeOffset Timestamp, LogLevel Level, string Category, string Message, bool IsFromPreviousRun = false)
{
    public string TimeText => Timestamp.ToString("HH:mm:ss.fff");

    public string LevelText => Level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "---"
    };

    /// <summary>Коротка назва категорії: RailwayErp.MvvmDemo.App → App.</summary>
    public string Source => Category[(Category.LastIndexOf('.') + 1)..];

    /// <summary>Записи попереднього запуску (прочитані з файлу) показуємо приглушено.</summary>
    public string SourceText => IsFromPreviousRun ? $"{Source} · попер. запуск" : Source;

    public double DisplayOpacity => IsFromPreviousRun ? 0.55 : 1.0;

    public string ToFileLine() =>
        $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{LevelText}|{Category}|{Message.Replace(Environment.NewLine, " ")}";

    public static LogEntry? TryParseFileLine(string line)
    {
        var parts = line.Split('|', 4);
        if (parts.Length != 4 || !DateTimeOffset.TryParse(parts[0], out var timestamp))
            return null;

        var level = parts[1] switch
        {
            "TRC" => LogLevel.Trace,
            "DBG" => LogLevel.Debug,
            "WRN" => LogLevel.Warning,
            "ERR" => LogLevel.Error,
            "CRT" => LogLevel.Critical,
            _ => LogLevel.Information
        };

        return new LogEntry(timestamp, level, parts[2], parts[3], IsFromPreviousRun: true);
    }
}
