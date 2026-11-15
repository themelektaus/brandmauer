using System.Net;
using System.Text;
using FileExtensionContentTypeProvider
    = Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider;

namespace Brandmauer;

public class ShareMiddleware(RequestDelegate next)
{
    public static readonly string[] PATHS = [ "/share", "/-" ];

    public async Task Invoke(HttpContext context)
    {
        Utils.LogBegin<ShareMiddleware>(context);

        var request = context.Request;
        var path = request.Path.ToString();
        var response = context.Response;

        var contextParameters = new ContextParameters(in PATHS, path);
        var _fileIndex = contextParameters.fileIndex;
        var _token = contextParameters.token;
        var _password = contextParameters.password;

        // Viewing a share by its link needs no login. Creating one (upload
        // page and POST, see UploadAsync) passes the checks of the route.
        var isSharePage = _token is not null
            && !_fileIndex.HasValue
            && FindShare(_token) is not null;

        if (isSharePage)
        {
            context.Features.Set(new PermissionFeature { Authorized = true });
        }

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
        var permission = context.Features.Get<PermissionFeature>();
        if (permission is { Authorized: false })
            return default;

        var request = context.Request;
        var path = request.Path.ToString();

        var contextParameters = new ContextParameters(in PATHS, path);
        var _fileIndex = contextParameters.fileIndex;
        var _token = contextParameters.token;
        var _password = contextParameters.password;

        if (IsUploadPage(path) && !HttpMethods.IsPost(request.Method))
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
            var share = FindShare(_token);
            if (share is null)
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

                var fileName = share.Files[i].Value;
                var size = info.Exists ? info.Length.ToHumanizedSize() : "-";
                var tooltip = WebUtility.HtmlEncode(
                    $"<div>{WebUtility.HtmlEncode(fileName)}</div>" +
                    $"<div><b>{size}</b></div>"
                );
                var name = WebUtility.HtmlEncode(
                    Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ')
                );

                var url = WebUtility.HtmlEncode(GetFileDownloadLink(share, i));

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
                    { "text", WebUtility.HtmlEncode(share.Text) },
                    { "file-list", fileListHtml.ToString() },
                    { "content", "<!--segment: share-->" }
                }
            };
        }

        return default;
    }

    static bool IsUploadPage(string path)
    {
        return PATHS.Any(x => x == path.TrimEnd('/'));
    }

    static Share FindShare(string token)
    {
        var share = Database.Use(
            x => x.Shares.FirstOrDefault(
                y => y.Token == token
            )
        );

        if (share is null)
            return null;

        share.OnDeserialize();
        if (share.Lifetime > 0 && share.expiresIn.HasExpired())
            return null;

        return share;
    }

    static string GetFileDownloadLink(Share share, int? i)
    {
        var url = $"{PATHS.LastOrDefault()}/{share.Token}";

        if (i.HasValue)
        {
            var fileName = WebUtility.UrlEncode(share.Files[i.Value].Value);
            url += $"/{i.Value}/{fileName}";
        }

        if (share.Password != string.Empty)
            url += $"${share.Password}";

        return url;
    }

    // Mapped as endpoint (POST /share, POST /-), so it runs only after the
    // checks of the route, like the API.
    public static async Task<IResult> UploadAsync(HttpRequest request)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest();

        var form = await request.ReadFormAsync();

        if (!form.TryGetValue("text", out var text))
            text = string.Empty;

        var formFiles = form.Files;

        if (text == string.Empty && formFiles.Count == 0)
            return Results.BadRequest();

        var share = Database.Use(x =>
        {
            var newData = x.Create<Share>();
            x.Shares.Add(newData);
            return newData;
        });

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
            await stream.CopyToAsync(fileStream);

            share.Files.Add(file);
        }

        return Results.Text(GetFileDownloadLink(share, null));
    }

    public class ContextParameters
    {
        public readonly string token;
        public readonly int? fileIndex;
        public readonly string password = string.Empty;

        public ContextParameters(in string[] basePaths, string path)
        {
            var subPaths = basePaths.Select(x => $"{x}/").ToArray();

            foreach (var subPath in subPaths)
            {
                if (!path.StartsWith(subPath))
                    continue;

                var tokenLength = Share.TOKEN_LENGTH;
                var length = subPath.Length + tokenLength;

                if (path.Length < length)
                    continue;

                token = path.Substring(subPath.Length, tokenLength);

                var parts = path.Split('$', 2);
                if (parts.Length == 2)
                    password = parts[1];

                path = parts[0];
                if (path.Length < length + 2)
                    continue;

                var fileIndexString = path[(length + 1)..].Split('/', 2)[0];
                if (!int.TryParse(fileIndexString, out var fileIndex))
                    continue;

                this.fileIndex = fileIndex;
            }
        }
    }
}
