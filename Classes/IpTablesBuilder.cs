using System.Text;

namespace Brandmauer;

public class IpTablesBuilder
{
    public const string IPTABLES = "iptables";

    public List<IpTablesFilter> filterInputs = new();
    public List<IpTablesNat> natPreroutings = new();

    public bool clear;

    public override string ToString()
    {
        var sheet = new StringBuilder();

        AddTitleTo(sheet, "Default policies");
        var defaultJump = clear ? "ACCEPT" : "DROP";
        sheet.AppendLine($"   {IPTABLES} -P INPUT {defaultJump}");
        sheet.AppendLine($"   {IPTABLES} -P FORWARD ACCEPT");
        sheet.AppendLine($"   {IPTABLES} -P OUTPUT ACCEPT");
        AddSpaceTo(sheet);

        AddTitleTo(sheet, "Clear tables");
        foreach (var arg in new[] { 'F', 'X', 'Z' })
            sheet.AppendLine($"   {IPTABLES} -{arg}");
        AddSpaceTo(sheet);

        AddTitleTo(sheet, "Clear chains");
        foreach (var chain in new[] { "nat", "mangle", "raw", "security" })
            foreach (var arg in new[] { 'F', 'X' })
                sheet.AppendLine($"   {IPTABLES} -t {chain} -{arg}");
        AddSpaceTo(sheet);

        if (clear)
            return sheet.ToString();

        foreach (var input in filterInputs)
        {
            var dir = " " + (
                input.direction == "INPUT"
                ? "→"
                : input.direction == "OUTPUT"
                    ? "←"
                    : "?"
            );
            
            var result = input.jump == "ACCEPT"
                ? "✅"
                : input.jump == "DROP"
                    ? "⛔"
                    : "?";
            
            var destinationPort = input.destinationPort ?? "any";
            
            AddTo(sheet, input.ToString(), [
                input.enabled ? ["", ""] : ["(DISABLED)"],
                ["Source:", input.source],
                [dir, $"{destinationPort} ({input.protocol}) {result}"],
            ]);
        }

        AddTitleTo(sheet, "Allow all established connections");
        sheet.Append($"   {IPTABLES} -A INPUT -m conntrack \\\n");
        sheet.AppendLine("    --ctstate ESTABLISHED,RELATED -j ACCEPT");
        AddSpaceTo(sheet);

        foreach (var prerouting in natPreroutings)
        {
            var destinationPort = prerouting.destinationPort;
            var protocol = prerouting.protocol;
            var destination = prerouting.destination;
            
            AddTo(sheet, prerouting.ToString(), [
                prerouting.enabled ? ["", ""] : ["(DISABLED)"],
                ["Name", prerouting.name],
                ["Source:", prerouting.source],
                ["", $"{destinationPort} ({protocol}) → {destination}"],
            ]);
        }

        AddTitleTo(sheet, "Masquerade everything");
        sheet.AppendLine($"   {IPTABLES} -t nat -A POSTROUTING -j MASQUERADE");
        AddSpaceTo(sheet);

        return sheet.ToString().Replace("\r\n", "\n");
    }

    static void AddTitleTo(StringBuilder sheet, string title)
    {
        AddSeparatorTo(sheet);
        sheet.AppendLine($"# :: {title}");
        AddSeparatorTo(sheet);
    }

    static void AddSpaceTo(StringBuilder sheet)
    {
        AddTo(sheet);
        AddTo(sheet);
    }

    static void AddTo(StringBuilder sheet)
    {
        sheet.AppendLine();
    }

    static void AddTo(StringBuilder sheet, string value)
    {
        AddTo(sheet, value, []);
    }

    static void AddTo(StringBuilder sheet, string value, string[][] comments)
    {
        AddSeparatorTo(sheet);

        var _comments = comments.Where(x
            => x.Length <= 1
            || !string.IsNullOrEmpty(x[1])
        ).ToList();
        
        if (_comments.Count == 0)
            _comments.Add(null);

        var labelSize = _comments.Max(x => x is null ? 0 : x[0].Length);
        foreach (var c in _comments)
        {
            var label = c is null || c.Length == 0 ? "" : c[0];
            while (label.Length < labelSize)
                label = ' ' + label;

            sheet.AppendLine($"# {label} {(c?.Length > 1 ? c[1] : "")}");
        }

        AddSeparatorTo(sheet);

        sheet.AppendLine($"   {value}");

        AddTo(sheet);
        AddTo(sheet);
    }

    static void AddSeparatorTo(StringBuilder sheet)
    {
        sheet.AppendLine($"#{Enumerable.Repeat('-', 67).Join()}");
    }
}
