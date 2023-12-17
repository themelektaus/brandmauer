namespace Brandmauer;

public static partial class Endpoint
{
    public static class Pushers
    {
        public static IResult GetAll()
        {
            var data = Database.Use(x => x.Pushers)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id);

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(
                x => x.Pushers.FirstOrDefault(
                    x => x.Identifier.Id == id
                )
            );

            if (data is null)
                return Results.NotFound();

            return Results.Json(data);
        }

        public static IResult Post()
        {
            var data = Database.Use(x =>
            {
                var newData = x.Create<Pusher>();
                newData.Reset();
                x.Pushers.Add(newData);
                x.Save();
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(Pusher data)
        {
            Database.Use(x =>
            {
                var index = x.Pushers.FindIndex(data.IsReferenceOf);
                x.Pushers[index] = x.Replace(x.Pushers[index], data);
                x.Pushers[index].Reset();
                x.Save();
            });

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.Pushers.RemoveAll(x =>
                {
                    x.Dispose();
                    return x.Identifier.Id == id;
                });
                x.Save();
            });
            return Results.Ok();
        }
    }
}
