namespace Brandmauer;

public static partial class Endpoint
{
    public static class Monitors
    {
        public static IResult GetAll()
        {
            var data = Database.Use(x => x.Monitors)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id);

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(
                x => x.Monitors.FirstOrDefault(
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
                var newData = x.Create<Monitor>();
                newData.Reset();
                x.Monitors.Add(newData);
                x.Save(logging: true);
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(Monitor data)
        {
            Database.Use(x =>
            {
                var index = x.Monitors.FindIndex(data.IsReferenceOf);
                x.Monitors[index] = x.Replace(x.Monitors[index], data);
                x.Monitors[index].Reset();
                x.Save(logging: true);
            });

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.Monitors.RemoveAll(x =>
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
