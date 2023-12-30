using Microsoft.AspNetCore.StaticFiles;

using System.Text;

namespace Brandmauer;

public class ShareMiddleware(RequestDelegate next)
{
    const string PATH = "/share";

    public class ContextParameters
    {
        public readonly HttpRequest request;
        public readonly string path;
        public readonly string token;
        public readonly int? fileIndex;

        public ContextParameters(HttpContext context)
        {
            request = context.Request;
            path = request.Path.ToString();

            var subPath = $"{PATH}/";

            if (!path.StartsWith(subPath))
                return;

            var tokenLength = Utils.DEFAULT_TOKEN_LENGTH;
            var length = subPath.Length + tokenLength;

            if (path.Length < length)
                return;

            token = path.Substring(subPath.Length, tokenLength);

            if (path.Length < length + 2)
                return;

            if (!int.TryParse(path[(length + 1)..], out var fileIndex))
                return;

            this.fileIndex = fileIndex;
        }
    }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<ShareMiddleware>(context);

        var p = new ContextParameters(context);
        if (p.path.StartsWith(PATH))
            context.Features.Set(new AuthorizedFeature());

        var response = context.Response;

        if (p.path == PATH && p.request.Method == "POST")
        {
            var upload = Upload(p.request);
            if (upload.statusCode == 200)
            {
                await response.Body.LoadFromAsync(upload.token);
                goto Exit;
            }

            goto Next;
        }

        if (p.fileIndex.HasValue)
        {
            var share = Database.Use(
                x => x.Shares.FirstOrDefault(y => y.Token == p.token)
            );
            if (share is null)
            {
                response.StatusCode = 404;
                goto Next;
            }

            var fileIndex = p.fileIndex.Value;
            var fileName = share.Files[fileIndex].Value;
            var filePath = share.GetLocalFilePath(fileIndex);
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                response.StatusCode = 404;
                goto Next;
            }

            response.Clear();

            var h = response.Headers;
            h.Append("Content-Disposition", $"inline; filename={fileName}");
            h.Append("Content-Length", fileInfo.Length.ToString());
            h.Append("Content-Transfer-Encoding", "binary");

            var provider = new FileExtensionContentTypeProvider();
            var mappings = provider.Mappings;
            mappings.TryGetValue(fileInfo.Extension, out var contentType);

            response.ContentType = contentType ?? "application/octet-stream";

            await response.SendFileAsync(filePath);

            goto Exit;
        }

    Next:
        await next.Invoke(context);

    Exit:
        Utils.LogOut<ShareMiddleware>(context);
    }

    [Frontend]
    static FrontendResult GetFrontend(HttpContext context)
    {
        var p = new ContextParameters(context);

        if (p.request.Path == PATH)
        {
            return new()
            {
                path = "prompt.html",
                replacements = new()
                {
                    { "content", "<!--segment: share-upload-->" }
                }
            };
        }

        if (p.token is not null && !p.fileIndex.HasValue)
        {
            var share = Database.Use(
                x => x.Shares.FirstOrDefault(y => y.Token == p.token)
            );
            var baseUrl = Database.Use(x => x.GetBaseUrl(p.request));

            var fileListHtml = new StringBuilder();

            for (int i = 0; i < share.Files.Count; i++)
            {
                fileListHtml.AppendLine(
                    "<div>" +
                        "<div>" +
                            share.Files[i].Value +
                        "</div>" +
                        $"<button data-url=\"{baseUrl}{PATH}/{share.Token}/{i}\">" +
                            "<i class=\"fas fa-download\"></i>" +
                            "<div>Download</div>" +
                        "</button>" +
                    "</div>"
                );
            }

            return new()
            {
                path = "prompt.html",
                replacements = new()
                {
                    { "file-list", fileListHtml.ToString() },
                    { "content", "<!--segment: share-download-->" }
                }
            };
        }

        return default;
    }

    static (int statusCode, string token) Upload(HttpRequest request)
    {
        if (!request.HasFormContentType)
            return (400, null);

        var formFiles = request.Form.Files;
        if (formFiles.Count == 0)
            return (400, null);

        var share = Database.Use(x =>
        {
            var newData = x.Create<Share>();
            x.Shares.Add(newData);
            return newData;
        });

        foreach (var formFile in formFiles)
        {
            var fileName = formFile.FileName;
            var file = new StringValue(fileName);
            var path = share.GetLocalFilePath(
                share.Files.Count + 1,
                fileName
            );

            using var stream = formFile.OpenReadStream();
            using var fileStream = File.Create(path);
            stream.CopyTo(fileStream);

            share.Files.Add(file);
        }

        var baseUrl = Database.Use(x =>
        {
            x.Save(logging: true);
            return x.GetBaseUrl(request);
        });

        return (200, share.Token);
    }
}
