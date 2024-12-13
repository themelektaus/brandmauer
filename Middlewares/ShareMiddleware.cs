﻿using System.Net;
using System.Text;
using FileExtensionContentTypeProvider
    = Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider;

namespace Brandmauer;

public class ShareMiddleware(RequestDelegate next)
{
    const string PATH = "/share";

    public async Task Invoke(HttpContext context)
    {
        Utils.LogBegin<ShareMiddleware>(context);

        var request = context.Request;
        var path = request.Path.ToString();

        if (path.StartsWith(PATH))
            context.Features.Set(new PermissionFeature { Authorized = true });

        var response = context.Response;

        if (path == PATH && request.Method == "POST")
        {
            var (statusCode, token) = Upload(request);
            if (statusCode == 200)
            {
                await response.Body.LoadFromAsync(token);
                goto Exit;
            }

            if (statusCode == 400)
            {
                response.StatusCode = 400;
                goto Exit;
            }

            goto Next;
        }

        var contextParameters = new ContextParameters(PATH, path);
        var _fileIndex = contextParameters.fileIndex;
        var _token = contextParameters.token;
        var _password = contextParameters.password;

        if (_fileIndex.HasValue)
        {
            var share = Database.Use(
                x => x.Shares.FirstOrDefault(
                    y => y.Token == _token && y.Password == _password
                )
            );

            if (share is null)
            {
                response.StatusCode = 404;
                goto Next;
            }

            share.OnDeserialize();
            if (share.Lifetime > 0 && share.expiresIn.HasExpired())
            {
                response.StatusCode = 404;
                goto Next;
            }

            var fileIndex = _fileIndex.Value;
            var fileName = Uri.EscapeDataString(share.Files[fileIndex].Value);
            var filePath = share.GetLocalFilePath(fileIndex);
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                response.StatusCode = 404;
                goto Next;
            }

            response.Clear();

            var h = response.Headers;
            h.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
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
        Utils.LogEnd<ShareMiddleware>(context);
    }

    [Frontend]
    static FrontendResult GetFrontend(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path.ToString();

        var contextParameters = new ContextParameters(PATH, path);
        var _fileIndex = contextParameters.fileIndex;
        var _token = contextParameters.token;
        var _password = contextParameters.password;

        if (path.TrimEnd('/') == PATH)
        {
            return new()
            {
                path = "prompt.html",
                replacements = new()
                {
                    { "upload-display", "inherit" },
                    { "password-display", "none" },
                    { "download-display", "none" },
                    { "create-another-share-display", "none" },
                    { "content", "<!--segment: share-->" }
                }
            };
        }

        if (_token is not null && !_fileIndex.HasValue)
        {
            var share = Database.Use(
                x => x.Shares.FirstOrDefault(
                    y => y.Token == _token
                )
            );

            if (share is null)
                return default;

            share.OnDeserialize();
            if (share.Lifetime > 0 && share.expiresIn.HasExpired())
                return default;

            if (share.Password != _password)
            {
                return new()
                {
                    path = "prompt.html",
                    replacements = new()
                    {
                        { "upload-display", "none" },
                        { "password-display", "inherit" },
                        { "download-display", "none" },
                        { "create-another-share-display", "none" },
                        { "content", "<!--segment: share-->" }
                    }
                };
            }

            var fileListHtml = new StringBuilder();

            for (int i = 0; i < share.Files.Count; i++)
            {
                var info = new FileInfo(share.GetLocalFilePath(i));

                var ext = info.Extension.TrimStart('.');
                var iconUrl = $"icon?file-extension={ext}";

                var name = share.Files[i].Value;
                var tooltip = $"<div>{name}</div>" +
                    $"<div><b>{info.Length.ToHumanizedSize()}</b></div>";
                name = Path.GetFileNameWithoutExtension(name).Replace('_', ' ');

                var url = GetFileDownloadLink(string.Empty, share, i);

                fileListHtml.AppendLine(
                    $" <div>                                               " +
                    $"   <div>                                             " +
                    $"     <div style='background: url({iconUrl})'></div>  " +
                    $"     <div>{name}</div>                               " +
                    $"   </div>                                            " +
                    $"   <button data-url=\"{url}\"                        " +
                    $"           data-tooltip=\"{tooltip}\">               " +
                    $"     <i class=\"fas fa-download\"></i>               " +
                    $"   </button>                                         " +
                    $" </div>                                              "
                );
            }

            bool createAnotherShareDisplay;

            if (request.Headers.TryGetValue("X-Source", out var source))
            {
                var _source = source.ToString().Split('/', 2).LastOrDefault(string.Empty);
                createAnotherShareDisplay = _source == string.Empty;
            }
            else
            {
                createAnotherShareDisplay = true;
            }

            return new()
            {
                path = "prompt.html",
                replacements = new()
                {
                    { "upload-display", "none" },
                    { "password-display", "none" },
                    { "download-display", "inherit" },
                    {
                        "create-another-share-display",
                        createAnotherShareDisplay ? "inherit" : "none"
                    },
                    { "text", share.Text },
                    { "file-list", fileListHtml.ToString() },
                    { "content", "<!--segment: share-->" }
                }
            };
        }

        return default;
    }

    static string GetFileDownloadLink(string baseUrl, Share share, int? i)
    {
        var url = $"{baseUrl}{PATH}/{share.Token}";

        if (i.HasValue)
        {
            var fileName = WebUtility.UrlEncode(share.Files[i.Value].Value);
            url += $"/{i.Value}/{fileName}";
        }

        if (share.Password != string.Empty)
            url += $"${share.Password}";

        return url;
    }

    static (int statusCode, string token) Upload(HttpRequest request)
    {
        if (!request.HasFormContentType)
            return (400, null);

        if (!request.Form.TryGetValue("text", out var text))
            text = string.Empty;

        var formFiles = request.Form.Files;

        if (text == string.Empty && formFiles.Count == 0)
            return (400, null);

        var share = Database.Use(x =>
        {
            var newData = x.Create<Share>();
            x.Shares.Add(newData);
            return newData;
        });

        var form = request.Form;

        share.Text = text;
        share.Lifetime = form.TryGetValue("lifetime", out var lifetime)
            ? int.TryParse(lifetime, out var _lifetime) ? _lifetime : 0 : 0;
        share.Password = form.TryGetValue("password", out var password)
            ? password : string.Empty;

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

        return (200, GetFileDownloadLink(baseUrl, share, null));
    }

    public class ContextParameters
    {
        public readonly string token;
        public readonly int? fileIndex;
        public readonly string password = string.Empty;

        public ContextParameters(string basePath, string path)
        {
            var subPath = $"{basePath}/";

            if (!path.StartsWith(subPath))
                return;

            var tokenLength = Utils.DEFAULT_TOKEN_LENGTH;
            var length = subPath.Length + tokenLength;

            if (path.Length < length)
                return;

            token = path.Substring(subPath.Length, tokenLength);

            var parts = path.Split('$', 2);
            if (parts.Length == 2)
                password = parts[1];

            path = parts[0];
            if (path.Length < length + 2)
                return;

            var fileIndexString = path[(length + 1)..].Split('/', 2)[0];
            if (!int.TryParse(fileIndexString, out var fileIndex))
                return;

            this.fileIndex = fileIndex;
        }
    }
}
