using Microsoft.AspNetCore.Mvc;

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
            public readonly string TimestampString
                => timestamp.ToHumanizedString();
        }

        public static IResult Get([FromQuery] int? limit) => GetById(0, limit);
        public static IResult GetById(long id, [FromQuery] int? limit)
        {
            var _limit = limit ?? 0;

            var ids = Enumerable.Empty<long>();

            if (Directory.Exists(_Audit.FOLDER))
            {
                ids = ids.Concat(
                    Directory.GetFiles(_Audit.FOLDER, "*.json")
                        .Select(Path.GetFileNameWithoutExtension)
                        .Select(x => long.TryParse(x, out var id) ? id : 0)
                        .Where(x => x != 0)
                        .OrderByDescending(x => x)
                );
            }

            if (_limit > 0)
                ids = ids.Take(_limit);

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
                    audit.Entries = [.. audit.Entries];
                }
                else
                {
                    audit = new();
                }
            }

            if (_limit > 0)
                audit.Entries = [.. audit.Entries.TakeLast(_limit)];

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
