namespace Brandmauer;

public static partial class Endpoint
{
    public static class Shares
    {
        public static IResult GetAll()
        {
            return Results.Json(EnumerateAll());
        }

        static IEnumerable<Share> EnumerateAll()
        {
            return Database.Use(x => x.Shares)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id);
        }

        public static IResult Get(long id)
        {
            var data = GetById(id);

            if (data is null)
                return Results.NotFound();

            return Results.Json(data);
        }

        static Share GetById(long id)
        {
            return Database.Use(
                x => x.Shares.FirstOrDefault(
                    x => x.Identifier.Id == id
                )
            );
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                var shares = x.Shares.Where(x => x.Identifier.Id == id);
                foreach (var s in shares)
                    for (var i = 0; i < s.Files.Count; i++)
                        try { File.Delete(s.GetLocalFilePath(i)); } catch { }

                x.Shares.RemoveAll(x =>
                {
                    x.Dispose();
                    return x.Identifier.Id == id;
                });
                x.Save(logging: true);
            });
            return Results.Ok();
        }
    }
}
