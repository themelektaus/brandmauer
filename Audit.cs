namespace Brandmauer;

public class Audit
{
    public static readonly string FOLDER = Path.Combine("Data", "Audit");

    static readonly Audit instance = new();

    public enum Status { Info, Warning, Error }

    public class Entry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string TimestampString
            => Timestamp.ToString("dd.MM.yyyy HH:mm:ss");

        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Status Status { get; set; }
    }
    readonly object handle = new();
    public List<Entry> Entries { get; set; } = new();

    public static List<Entry> GetAll()
    {
        List<Entry> entries;

        lock (instance.handle)
        {
            entries = [.. instance.Entries];
        }

        return entries;
    }

    public static void Save()
    {
        lock (instance.handle)
        {
            instance.SaveInternal();
        }
    }

    public static void CleanUp()
    {
        lock (instance.handle)
        {
            var yesterday = DateTime.Now.AddDays(-1);
            instance.Entries.RemoveAll(x => x.Timestamp < yesterday);
        }
    }

    public static void Info<T>(object message)
        => Info(typeof(T), message);
    public static void Info(Type type, object message)
        => Info(type?.Name, message);
    public static void Info(string type, object message)
        => instance.Add(type, message, Status.Info);

    public static void Warning<T>(object message)
        => Warning(typeof(T), message);
    public static void Warning(Type type, object message)
        => Warning(type?.Name, message);
    public static void Warning(string type, object message)
        => instance.Add(type, message, Status.Warning);

    public static void Error<T>(object message)
        => Error(typeof(T), message);
    public static void Error(Type type, object message)
        => Error(type?.Name, message);
    public static void Error(string type, object message)
        => instance.Add(type, message, Status.Error);

    void Add(string type, object message, Status status)
    {
        lock (handle)
        {
            Entries.Add(new()
            {
                Type = type ?? Utils.Name,
                Message = message?.ToString() ?? string.Empty,
                Status = status
            });
        }
    }

    void SaveInternal()
    {
        Info<Audit>("Saving...");

        var name = DateTime.Now.Ticks;

        for (; ; )
        {
            var file = Path.Combine(FOLDER, $"{name}.json");

            if (File.Exists(file))
            {
                name++;
                continue;
            }

            try { Directory.CreateDirectory(FOLDER); } catch { }

            Info<Audit>("Saved.");
            File.WriteAllText(file, this.ToJson());

            break;
        }
    }
}
