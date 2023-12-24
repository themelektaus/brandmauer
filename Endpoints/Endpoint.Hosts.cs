using System.Text;

namespace Brandmauer;

public static partial class Endpoint
{
    public static class Hosts
    {
        public static IResult GetAll(HttpRequest request)
        {
            if (request.Query.TryGetValue("format", out var format))
                if (format == "html")
                    return GetAllAsHtml();

            return Results.Json(EnumerateAll());
        }

        static IResult GetAllAsHtml()
        {
            var data = EnumerateAll().ToList();

            var html = new StringBuilder();

            foreach (var item in data)
            {
                html.Append(
                    $"<div>" +
                    $"<div>{item.HtmlName}</div>" +
                    $"<div>{item.HtmlInfo}</div>" +
                    $"</div>"
                );
            }

            return Results.Content(html.ToString(), "text/html");
        }

        static IEnumerable<Host> EnumerateAll()
        {
            return Database.Use(x => x.Hosts)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(
                x => x.Hosts.FirstOrDefault(
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
                var newData = x.Create<Host>();
                x.Hosts.Add(newData);
                x.Save(logging: true);
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(Host data)
        {
            Database.Use(x =>
            {
                var index = x.Hosts.FindIndex(data.IsReferenceOf);
                x.Hosts[index] = x.Replace(x.Hosts[index], data);
                x.Save(logging: true);
            });

            ReverseProxyPreparatorMiddleware.targetCache.Clear();

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.Hosts.RemoveAll(x =>
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
