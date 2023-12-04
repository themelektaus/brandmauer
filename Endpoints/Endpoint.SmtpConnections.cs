using Microsoft.AspNetCore.Mvc;
using System.Text;

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
                x => x.SmtpConnections.FirstOrDefault(y => y.Identifier.Id == id)
            );

            if (smtpConnection is null)
                return Results.BadRequest();

            var client = new SmtpClient
            {
                host = smtpConnection.Host,
                port = smtpConnection.Port,
                username = smtpConnection.Username,
                password = smtpConnection.Password,
                from = smtpConnection.Sender == string.Empty
                    ? smtpConnection.Username
                    : smtpConnection.Sender,
                to = [.. to],
                subject = subject,
                body = body,
                tls = smtpConnection.Tls,
                html = false
            };

            var result = new StringBuilder();

            result.AppendLine("-- Begin client options --");
            result.AppendLine($"Host: {client.host}");
            result.AppendLine($"Port: {client.port}");
            result.AppendLine($"TLS/SSL: {(client.tls ? "Yes" : "No")}");
            result.AppendLine($"From: {client.from}");
            result.AppendLine($"To: {client.to.Join(", ")}");
            result.AppendLine($"Subject: {client.subject}");
            result.AppendLine($"Body: {client.body}");
            result.AppendLine("-- End client options --");

            var error = false;

            try
            {
                var response = await client.Send();

                if (response.sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                {
                    result.AppendLine("[OK] Success");
                }
                else
                {
                    result.AppendLine("[WARNING] Success, with SSL Policy Errors");
                    result.AppendLine(response.sslPolicyErrors.ToString());
                }

                result.AppendLine(response.result);
            }
            catch (Exception ex)
            {
                result.AppendLine("[ERROR]");
                result.AppendLine(ex.Message);
                error = true;
            }

            var text = result.ToString();
            return Results.Text(text, statusCode: error ? 400 : 200);
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
