namespace Brandmauer;

public static partial class Endpoint
{
	public static class IpTables
	{
		public static IResult Get(HttpRequest request)
		{
			var result = ShellCommand.Execute(@$"
				iptables -S
				echo """"
				iptables -S -t nat
				echo """"
				iptables -S -t mangle
				echo """"
				iptables -S -t raw
				echo """"
				iptables -S -t security
			");

			var lists = new List<string[]>();
			
			foreach (var block in result.StdOut.Split("\n\n"))
				lists.Add(block.Split('\n'));

			while (lists.Count < 5)
				lists.Add([]);

			result.Data = new
			{
				filter = lists[0],
				nat = lists[1],
				mangle = lists[2],
				raw = lists[3],
				security = lists[4],
			};

			return result.ToResult(request);
		}
	}
}
