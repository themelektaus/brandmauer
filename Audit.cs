namespace Brandmauer;

public class Audit
{
    public static readonly string FOLDER = Path.Combine("Data", "Audit");

    public static Audit Instance { get; } = new();

    public enum Status { Info, Warning, Error }

    public class Entry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Status Status { get; set; }
    }
    readonly object entriesLock = new();
    public List<Entry> Entries { get; set; } = new();

    public static IList<Entry> GetAll()
    {
        lock (Instance.entriesLock)
            return Instance.Entries.ToList();
    }

    public static void Info<T>(object message) => Info(typeof(T), message);
    public static void Info(Type type, object message) => Info(type?.Name, message);
    public static void Info(string type, object message) => Instance.Add(type, message, Status.Info);

    public static void Warning<T>(object message) => Warning(typeof(T), message);
    public static void Warning(Type type, object message) => Warning(type?.Name, message);
    public static void Warning(string type, object message) => Instance.Add(type, message, Status.Warning);

    public static void Error<T>(object message) => Error(typeof(T), message);
    public static void Error(Type type, object message) => Error(type?.Name, message);
    public static void Error(string type, object message) => Instance.Add(type, message, Status.Error);

    void Add(string type, object message, Status status)
    {
        lock (entriesLock)
            Entries.Add(new()
            {
                Type = type ?? Utils.Name,
                Message = message?.ToString() ?? string.Empty,
                Status = status
            });
    }

    public void Save()
    {
        Info<Audit>("Saving...");

        var x = DateTime.Now.Ticks;
        for (; ; )
        {
            var file = Path.Combine("Data", "Audit", $"{x}.json");
            if (File.Exists(file))
            {
                x++;
                continue;
            }
            try { new FileInfo(file).Directory.Create(); } catch { }
            Info<Audit>("Saved.");
            File.WriteAllText(file, this.ToJson());
            break;
        }

    }
}
