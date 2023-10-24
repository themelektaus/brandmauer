namespace Brandmauer;

public static partial class Endpoint
{
    public static class ReverseProxyRoutes
    {
        public static IResult GetAll()
        {
            var data = Database.Use(x => x.ReverseProxyRoutes)
                .OrderBy(x => x.SourceDomains.FirstOrDefault().ToString())
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id);

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(
                x => x.ReverseProxyRoutes.FirstOrDefault(
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
                var newData = x.Create<ReverseProxyRoute>();
                x.ReverseProxyRoutes.Add(newData);
                x.Save();
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(ReverseProxyRoute data)
        {
            Database.Use(x =>
            {
                var index = x.ReverseProxyRoutes.FindIndex(data.IsReferenceOf);
                x.ReverseProxyRoutes[index] = x.Replace(x.ReverseProxyRoutes[index], data);
                x.Save();
            });

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.ReverseProxyRoutes.RemoveAll(x =>
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
