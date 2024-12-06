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

        public static IResult Get(long id, HttpRequest request)
        {
            var data = Database.Use(
                x => x.ReverseProxyRoutes.FirstOrDefault(
                    x => x.Identifier.Id == id
                )
            );

            if (data is null)
                return Results.NotFound();

            if (request.Query.TryGetValue("format", out var format))
                if (format == "html")
                    return GetAsHtml(data);

            return Results.Json(data);
        }

        static IResult GetAsHtml(ReverseProxyRoute route)
        {
            var content = route.Script;

            var html = File.ReadAllText("wwwroot/editor.html")
                .Replace("<!--id-->", route.Identifier.Id.ToString())
                .Replace("<!--title-->", route.SourceDomains.FirstOrDefault().Value)
                .Replace("<!--output-type-->", route.ScriptOutputType.ToString())
                .Replace("<!--script-->", content.ToBase64());

            return Results.Content(html, "text/html");
        }

        public static IResult Post()
        {
            var data = Database.Use(x =>
            {
                var newData = x.Create<ReverseProxyRoute>();
                x.ReverseProxyRoutes.Add(newData);
                x.Save(logging: true);
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(ReverseProxyRoute data)
        {
            Database.Use(x =>
            {
                var index = x.ReverseProxyRoutes.FindIndex(data.IsReferenceOf);
                data.Script = x.ReverseProxyRoutes[index].Script;
                x.ReverseProxyRoutes[index] = x.Replace(x.ReverseProxyRoutes[index], data);
                x.Save(logging: true);
            });

            return Results.Ok();
        }

        public static IResult PutScript(ReverseProxyRoute data)
        {
            Database.Use(x =>
            {
                var index = x.ReverseProxyRoutes.FindIndex(data.IsReferenceOf);
                x.ReverseProxyRoutes[index].Script = data.Script;
                x.Save(logging: true);
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
                x.Save(logging: true);
            });
            return Results.Ok();
        }
    }
}
