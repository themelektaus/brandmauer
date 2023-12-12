namespace Brandmauer;

public static partial class Endpoint
{
    public static class SmtpConnections
    {
        public static IResult GetAll()
        {
            var data = Database.Use(x => x.SmtpConnections)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id);

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(
                x => x.SmtpConnections.FirstOrDefault(
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
                var newData = x.Create<SmtpConnection>();
                x.SmtpConnections.Add(newData);
                x.Save();
                return newData;
            });

            return Results.Json(data);
        }

        public static async Task<IResult> SendAsync(HttpContext context)
        {
            var form = context.Request.Form;

            if (!form.TryGetValue("subject", out var subject))
                return Results.BadRequest();

            if (!form.TryGetValue("body", out var body))
                return Results.BadRequest();

            if (!form.TryGetValue("to", out var to))
                return Results.BadRequest();

            if (!form.TryGetValue("id", out var idString))
                return Results.BadRequest();

            if (!long.TryParse(idString, out var id))
                return Results.BadRequest();

            var smtpConnection = Database.Use(
                x => x.SmtpConnections.FirstOrDefault(
                    y => y.Identifier.Id == id
                )
            );

            if (smtpConnection is null)
                return Results.BadRequest();

            bool html = false;
            if (form.TryGetValue("html", out var htmlString))
                if (bool.TryParse(htmlString, out var _html))
                    html = _html;

            var (statusCode, text) = await smtpConnection
                .SendAsync([.. to], subject, body, html);

            return Results.Text(text, statusCode: statusCode);
        }

        public static IResult Put(SmtpConnection data)
        {
            Database.Use(x =>
            {
                var index = x.SmtpConnections.FindIndex(data.IsReferenceOf);
                x.SmtpConnections[index] = x.Replace(x.SmtpConnections[index], data);
                x.Save();
            });

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.SmtpConnections.RemoveAll(x =>
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
