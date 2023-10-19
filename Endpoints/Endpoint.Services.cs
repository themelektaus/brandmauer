namespace Brandmauer;

public static partial class Endpoint
{
    public static class Services
    {
        public static IResult GetAll()
        {
            var data = Database.Use(x => x.Services);

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(
                x => x.Services.FirstOrDefault(
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
                var newData = x.Create<Service>();
                x.Services.Add(newData);
                x.Save();
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(Service data)
        {
            Database.Use(x =>
            {
                var index = x.Services.FindIndex(data.IsReferenceOf);
                x.Services[index] = x.Replace(x.Services[index], data);
                x.Save();
            });

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.Services.RemoveAll(x =>
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
