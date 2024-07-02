using System.Text;

namespace Brandmauer;

public static partial class Endpoint
{
    public static class Shares
    {
        public static IResult GetAll(HttpRequest request)
        {
            if (request.Query.TryGetValue("format", out var format))
                if (format == "html")
                    return GetAllAsHtml();

            return Results.Json(ListAll());
        }

        static IResult GetAllAsHtml()
        {
            var rows = new StringBuilder();

            foreach (var item in ListAll())
            {
                var expires = item.expiresIn.Ticks > 0;

                var expiresOn = expires
                    ? item.expiresOn.ToHumanizedString()
                    : "";

                var expiresIn = item.expiresIn.Ticks > 0
                    ? item.expiresIn.ToHumanizedString()
                    : "Never";

                var files = item.Files.JoinWrap("<li>", "</li>");
                var text = item.Text;

                rows.AppendLine("<div class=\"row\">");
                rows.AppendLine($"<div class=\"expires-on\">{expiresOn}</div>");
                rows.AppendLine($"<div class=\"expires-in\">{expiresIn}</div>");
                rows.AppendLine($"<div class=\"files\"><ul>{files}</ul></div>");
                rows.AppendLine($"<div class=\"text\">{text}</div>");
                rows.AppendLine(
                    "<div class=\"link\">" +
                        $"<a href=\"share/{item.Token}\" target=\"_blank\">" +
                            "Open Share" +
                        $"</a>" +
                    "</div>"
                );

                rows.AppendLine("</div>");
            }

            var html = File.ReadAllText("wwwroot/shares-table.html")
                .Replace("<!--rows-->", rows.ToString());

            return Results.Content(html, "text/html");
        }

        static List<Share> ListAll()
        {
            var data = Database.Use(x => x.Shares)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id)
                .ToList();

            foreach (var item in data)
                item.OnDeserialize();

            return data;
        }

        public static IResult Get(long id)
        {
            var data = GetById(id);

            if (data is null)
                return Results.NotFound();

            data.OnDeserialize();
            return Results.Json(data);
        }

        public static IResult Put(Share data)
        {
            Database.Use(x =>
            {
                var index = x.Shares.FindIndex(data.IsReferenceOf);
                data.Files = x.Shares[index].Files;
                x.Shares[index] = x.Replace(x.Shares[index], data);
                x.Save(logging: true);
            });

            return Results.Ok();
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
