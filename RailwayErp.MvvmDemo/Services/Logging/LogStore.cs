using System.Collections.ObjectModel;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Services.Logging;

public interface ILogStore
{
    /// <summary>Записи журналу (новіші — зверху). Змінюється лише в UI-потоці.</summary>
    ObservableCollection<LogEntry> Entries { get; }

    /// <summary>Файл, у який дублюється журнал (переживає перезапуск застосунку).</summary>
    string LogFilePath { get; }

    void Add(LogEntry entry);

    void Clear();
}

/// <summary>
/// Сховище журналу: тримає записи в пам'яті для сторінки «Журнал» і дописує їх у файл.
/// Під час старту підвантажує хвіст файлу — так видно події попереднього запуску,
/// зокрема Window.Destroying, яку інакше неможливо побачити після закриття вікна.
/// </summary>
public sealed class LogStore : ILogStore
{
    private const int MaxEntries = 400;
    private const int PreviousRunLines = 30;
    private const long MaxFileBytes = 256 * 1024;

    private readonly object _fileLock = new();

    public LogStore()
    {
        LogFilePath = Path.Combine(FileSystem.AppDataDirectory, "lifecycle.log");
        LoadPreviousRun();
    }

    public ObservableCollection<LogEntry> Entries { get; } = [];

    public string LogFilePath { get; }

    public void Add(LogEntry entry)
    {
        AppendToFile(entry);

        if (MainThread.IsMainThread)
            Insert(entry);
        else
            MainThread.BeginInvokeOnMainThread(() => Insert(entry));
    }

    public void Clear()
    {
        Entries.Clear();
        lock (_fileLock)
        {
            File.WriteAllText(LogFilePath, string.Empty);
        }
    }

    private void Insert(LogEntry entry)
    {
        Entries.Insert(0, entry);
        while (Entries.Count > MaxEntries)
            Entries.RemoveAt(Entries.Count - 1);
    }

    private void AppendToFile(LogEntry entry)
    {
        try
        {
            lock (_fileLock)
            {
                File.AppendAllText(LogFilePath, entry.ToFileLine() + Environment.NewLine);
            }
        }
        catch (IOException)
        {
            // Журнал у файлі — допоміжний; помилка запису не повинна ламати застосунок.
        }
    }

    private void LoadPreviousRun()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
            if (!File.Exists(LogFilePath))
                return;

            var lines = File.ReadAllLines(LogFilePath);

            if (new FileInfo(LogFilePath).Length > MaxFileBytes)
                File.WriteAllLines(LogFilePath, lines.TakeLast(200));

            foreach (var line in lines.TakeLast(PreviousRunLines))
            {
                if (LogEntry.TryParseFileLine(line) is { } entry)
                    Entries.Insert(0, entry);
            }
        }
        catch (IOException)
        {
        }
    }
}
