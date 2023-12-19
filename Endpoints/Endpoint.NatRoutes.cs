#if LINUX
namespace Brandmauer;

public static partial class Endpoint
{
    public static class NatRoutes
    {
        public static IResult GetAll()
        {
            var data = Database.Use(x => x.NatRoutes)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id);

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(
                x => x.NatRoutes.FirstOrDefault(
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
                var newData = x.Create<NatRoute>();
                x.NatRoutes.Add(newData);
                x.Save();
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(NatRoute data)
        {
            Database.Use(x =>
            {
                var index = x.NatRoutes.FindIndex(data.IsReferenceOf);
                x.NatRoutes[index] = x.Replace(x.NatRoutes[index], data);
                x.Save();
            });

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.NatRoutes.RemoveAll(x =>
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
#endif
