namespace Brandmauer;

using _Audit = Audit;

public static partial class Endpoint
{
    public static class Audit
    {
        public struct Entry
        {
            public long id;
            public string path;
            public DateTime timestamp;
        }

        public static IResult Get() => GetById(0);
        public static IResult GetById(long id)
        {
            var ids = Directory.Exists(_Audit.FOLDER)
                ? Directory.GetFiles(_Audit.FOLDER, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Select(x => long.TryParse(x, out var id) ? id : 0)
                    .Where(x => x != 0)
                    .OrderByDescending(x => x)
                    .ToList()
                : [];

            _Audit audit;

            if (id == 0)
            {
                audit = new()
                {
                    Entries = _Audit.GetAll()
                };
            }
            else
            {
                var file = Path.Combine(_Audit.FOLDER, $"{id}.json");
                if (File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    audit = json.FromJson<_Audit>();
                    audit.Entries = audit.Entries
                        //.OrderByDescending(x => x.Timestamp)
                        .ToList();
                }
                else
                {
                    audit = new();
                }
            }

            return Results.Json(new
            {
                selected = audit,
                archive = Enumerable.Empty<Entry>().Append(
                    new Entry
                    {
                        id = 0,
                        path = "api/audit",
                        timestamp = DateTime.Now
                    }
                ).Concat(
                    ids.Select(id => new Entry
                    {
                        id = id,
                        path = $"api/audit/{id}",
                        timestamp = DateTime.FromBinary(id)
                    })
                )
            });
        }
    }
}
