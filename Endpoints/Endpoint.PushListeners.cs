namespace Brandmauer;

public static partial class Endpoint
{
    public static class PushListeners
    {
        public static IResult GetAll()
        {
            var data = Database.Use(x => x.PushListeners)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id);

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(
                x => x.PushListeners.FirstOrDefault(
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
                var newData = x.Create<PushListener>();
                newData.Touch();
                x.PushListeners.Add(newData);
                x.Save(logging: true);
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(PushListener data)
        {
            Database.Use(x =>
            {
                var index = x.PushListeners.FindIndex(data.IsReferenceOf);
                x.PushListeners[index] = x.Replace(x.PushListeners[index], data);
                x.PushListeners[index].Touch();
                x.Save(logging: true);
            });

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.PushListeners.RemoveAll(x =>
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
