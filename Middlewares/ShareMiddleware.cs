using Microsoft.AspNetCore.StaticFiles;

namespace Brandmauer;

public class ShareMiddleware(RequestDelegate next)
{
    const string PATH = "/share";

    public class ContextParameters
    {
        public readonly HttpRequest request;
        public readonly string path;
        public readonly string token;
        public readonly bool isDownload;

        public ContextParameters(HttpContext context)
        {
            request = context.Request;
            path = request.Path.ToString();
            
            var subPath = $"{PATH}/";
            if (path.StartsWith(subPath))
                if (path.Length == subPath.Length + Utils.DEFAULT_TOKEN_LENGTH)
                    token = path[subPath.Length..];

            isDownload = request.Query.TryGetValue("download", out _);
        }
    }

    public readonly struct SharedFile
    {
        public readonly string name;
        public readonly string path;

        public bool IsNull => name is null || path is null;

        SharedFile(string name, string path)
        {
            this.name = name;
            this.path = path;
        }

        public static SharedFile FindByToken(string token)
            => Database.Use(x =>
            {
                foreach (var share in x.Shares)
                {
                    var files = share.Files;
                    for (var i = 0; i < files.Count; i++)
                    {
                        if (files[i].Description != token)
                            continue;

                        return new SharedFile(
                            files[i].Value,
                            share.GetLocalFilePath(i)
                        );
                    }
                }
                return default;
            });
    }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<ShareMiddleware>(context);

        var p = new ContextParameters(context);

        var response = context.Response;

        if (p.path == PATH && p.request.Method == "POST")
        {
            var upload = Upload(p.request);
            if (upload.statusCode == 200)
            {
                await response.Body.LoadFromAsync(upload.downloadLink);
                goto Exit;
            }

            goto Next;
        }

        if (p.token is not null && p.isDownload)
        {
            var sf = SharedFile.FindByToken(p.token);
            if (sf.IsNull)
            {
                response.StatusCode = 404;
                goto Next;
            }

            var fileInfo = new FileInfo(sf.path);
            if (!fileInfo.Exists)
            {
                response.StatusCode = 404;
                goto Next;
            }

            response.Clear();

            var h = response.Headers;
            h.Append("Content-Disposition", $"inline; filename={sf.name}");
            h.Append("Content-Length", fileInfo.Length.ToString());
            h.Append("Content-Transfer-Encoding", "binary");

            var provider = new FileExtensionContentTypeProvider();
            var mappings = provider.Mappings;
            mappings.TryGetValue(fileInfo.Extension, out var contentType);

            response.ContentType = contentType ?? "application/octet-stream";

            await response.SendFileAsync(sf.path);

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

        if (p.token is not null && !p.isDownload)
        {
            var sf = SharedFile.FindByToken(p.token);
            var baseUrl = Database.Use(x => x.GetBaseUrl(p.request));

            return new()
            {
                path = "prompt.html",
                replacements = new()
                {
                    { "name", sf.name },
                    { "link", $"{baseUrl}{PATH}/{p.token}?download={sf.name}" },
                    { "content", "<!--segment: share-download-->" }
                }
            };
        }

        return default;
    }

    static (int statusCode, string downloadLink) Upload(HttpRequest request)
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

        var formFile = formFiles[0];

        var fileName = formFile.FileName;
        var file = new StringValue(fileName, Utils.GenerateToken());
        var path = share.GetLocalFilePath(
            share.Files.Count + 1,
            fileName
        );

        using var stream = formFile.OpenReadStream();
        using var fileStream = File.Create(path);
        stream.CopyTo(fileStream);

        share.Files.Add(file);

        var baseUrl = Database.Use(x =>
        {
            x.Save(logging: true);
            return x.GetBaseUrl(request);
        });

        return (200, file.Description);
    }
}
