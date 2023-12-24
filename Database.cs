using Microsoft.AspNetCore.Http.Extensions;

namespace Brandmauer;

public class Database
{
    public static readonly string databaseFile
        = Path.Combine("Data", "Database.json");

    public static bool Loading { get; private set; }
    public static DateTime LastKnownWriteTime { get; private set; }

    static readonly ThreadsafeContext context = new();
    static Database database;

    public List<Host> Hosts { get; set; } = new();
    public List<Service> Services { get; set; } = new();
    public List<Rule> Rules { get; set; } = new();
    public List<NatRoute> NatRoutes { get; set; } = new();
    public List<ReverseProxyRoute> ReverseProxyRoutes { get; set; } = new();
    public List<Certificate> Certificates { get; set; } = new();
    public List<Authentication> Authentications { get; set; } = new();
    public List<SmtpConnection> SmtpConnections { get; set; } = new();
    public List<Monitor> Monitors { get; set; } = new();
    public List<PushListener> PushListeners { get; set; } = new();
    public Config Config { get; set; } = new();

    static readonly HashSet<Model> models = new();

    public static void Register(Model model) => models.Add(model);
    public static void Unregister(Model model) => models.Remove(model);

    void PostDeserialize()
    {
        foreach (var model in models)
            model.PostDeserialize(this);
    }

    public static void Load()
    {
        Use(x =>
        {
            Audit.Info<Database>("Loading...");
            Loading = true;

            foreach (var model in models)
                model.Dispose();

            if (!File.Exists(databaseFile))
            {
                database = new();
                database.Save(logging: false);
            }

            var json = File.ReadAllText(databaseFile);
            database = json.FromJson<Database>();
            database.PostDeserialize();

            UpdateLastKnownWriteTime();

            Audit.Info<Database>("Loaded.");
            Loading = false;

            var ca = database.Certificates.FirstOrDefault(x => x.HasAuthority);
            if (ca is not null)
                return;

            Audit.Info<Database>("Generating CA.");

            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddYears(10);

            var _caName = Utils.Name;

            var _ca = CertificateUtils.CreateCertificate(
                $"CN={_caName}",
                startDate,
                endDate
            );

            ca = new()
            {
                Name = _caName,
                HasAuthority = true
            };
            ca.Write(database, _ca);
            database.Certificates.Add(ca);

            Audit.Info<Database>("CA created.");
        });
    }

    public void Save(bool logging)
    {
        if (logging)
            Audit.Info<Database>("Saving...");

        try { new FileInfo(databaseFile).Directory.Create(); } catch { }
        File.WriteAllText(databaseFile, this.ToJson());
        UpdateLastKnownWriteTime();

        PostDeserialize();

        if (logging)
            Audit.Info<Database>("Saved.");
    }

    static void UpdateLastKnownWriteTime()
    {
        LastKnownWriteTime = File.GetLastWriteTime(databaseFile);
    }

    public static void Use(Action<Database> action)
        => context.Use(() => action(database));

    public static T Use<T>(Func<Database, T> func)
        => context.Use(() => func(database));

    public static Task UseAsync(Func<Database, Task> task)
        => context.UseAsync(() => task(database));

    public static Task<T> UseAsync<T>(Func<Database, Task<T>> task)
        => context.UseAsync(() => task(database));

    public T Create<T>() where T : Model, new()
    {
        var newModel = new T();
        Register(newModel);
        return newModel;
    }

    public T Replace<T>(T oldModel, T newModel) where T : Model
    {
        oldModel.Dispose();
        Register(newModel);
        return newModel;
    }

    public string GetBaseUrl(HttpRequest request = null)
    {
        var baseUrl = Config.ExternalUrl;
        if (baseUrl != string.Empty)
            return baseUrl;

        if (request is null)
            return string.Empty;

        var url = request.GetDisplayUrl();
        var path = request.Path.ToString();
        return url[..^path.Length];
    }
}
