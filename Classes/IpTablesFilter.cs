namespace Brandmauer;

public class IpTablesFilter
{
	public string source;
	public string direction;
	public string protocol;
	public string destinationPort;
	public bool enabled;
	public string jump;

	public readonly List<string> errors = new();

	IpTablesFilter() { }

	public static List<IpTablesFilter> From(Rule rule)
	{
		var inputs = new List<IpTablesFilter>();

		var baseInput = new IpTablesFilter();

		if (!rule.TryGetSource(out baseInput.source))
		{
			baseInput.errors.Add("No hosts");
			inputs.Add(baseInput);
			goto Return;
		}

		foreach (var service in rule.Services)
		{
			foreach (var port in service.Ports)
			{
				var input = new IpTablesFilter();
		
				var range = port.GetRange(':');
				if (range is null)
				{
					input.errors.Add(port.ToString());
					continue;
				}
		
				input.enabled = rule.Enabled;
				
				switch (rule.Direction)
				{
					case Rule._Direction.Inbound:
						input.direction = "INPUT";
						break;
		
					case Rule._Direction.Outbound:
						input.direction = "OUTPUT";
						break;
				}
		
				input.protocol = port.Protocol.ToString().ToLower();
				input.source = baseInput.source;
		
				if (range != string.Empty)
					input.destinationPort = range;
		
				switch (rule.Action)
				{
					case Rule._Action.Allow:
						input.jump = "ACCEPT";
						break;
		
					case Rule._Action.Block:
						input.jump = "DROP";
						break;
				}
		
				inputs.Add(input);
			}
		}

	Return:
		return inputs;
	}

	public override string ToString()
	{
		if (errors.Count > 0)
			return $"# ERROR: {errors.Join(", ")}";

		var args = new List<string>();

        var e = $"{(enabled ? "" : "#")}";

		args.Add($"{e} {IpTablesBuilder.IPTABLES}");
		args.AddIfNotNull("-A ", direction);
        args.AddIfNotNull("-p ", protocol);
		args.AddIfNotNull("--dport ", destinationPort);
		args.AddIfNotNull("-j ", jump);
        args.AddIfNotNull($"\\\n    {e} -s ", source);

        return args.Join(' ');
	}
}
