namespace Brandmauer;

public static partial class Endpoint
{
	public static class Info
	{
		public static IResult Get()
		{
			var assemblyName = Utils.GetAssemblyName();
			var processFile = new FileInfo(Environment.ProcessPath);

			static object GetFileInfo(FileInfo file)
			{
				var path = file?.FullName ?? "";
				var timestamp = file?.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
				return new { path, timestamp };
			}

			var databaseFile = new FileInfo(Database.databaseFile);

			var wwwrootFiles = Directory
				.EnumerateFiles("wwwroot", "*.*", SearchOption.AllDirectories)
				.Select(x => new FileInfo(x))
				.ToList();

			var newestFile = wwwrootFiles
				.Append(processFile)
				.Where(x => x is not null)
				.OrderByDescending(x => x.LastWriteTime)
				.FirstOrDefault();

			return Results.Json(new
			{
				name = assemblyName.Name,
				version = assemblyName.Version.ToString(),
				newestFile = GetFileInfo(newestFile),
				process = GetFileInfo(processFile),
				wwwroot = wwwrootFiles.Select(GetFileInfo),
				database = GetFileInfo(databaseFile)
            });
		}

		public static IResult GetRequests(HttpRequest request)
		{
			IResult result;
			
			var requests = RequestInfo.GetAll();

			if (request.Query.TryGetValue("output", out var output))
			{
				if (output == "text")
				{
					var newLine = Enumerable.Repeat(Environment.NewLine, 5).Join();
					var text = requests.Select(x => x.ToString()).Join(newLine) + newLine;
					result = Results.Text(text);
					goto Result;
				}
			}

			result = Results.Json(requests);

		Result:
			RequestInfo.Clear();
			return result;
		}
	}
}
