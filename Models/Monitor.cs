using System.Reflection;

namespace Brandmauer;

public partial class Monitor : Model
{
    public bool Enabled { get; set; } = true;

    public enum _CheckType
    {
        None,
        Process,
        Service
    }

    public _CheckType CheckType { get; set; }

    public int Interval { get; set; } = 50;

    public string SuccessUrl { get; set; } = string.Empty;
    public List<StringValue> SuccessHeaders { get; set; } = new();

    DateTime? last;

    static readonly Dictionary<_CheckType, MethodInfo> checkMethods = new();

    static Monitor()
    {
        var methods = Utils.GetInstanceMethods();
        foreach (var value in Enum.GetValues<_CheckType>())
        {
            checkMethods.Add(
                value,
                methods.FirstOrDefault(
                    x => x.Name == $"Check_{Enum.GetName(value)}"
                )
            );
        }
    }

    public void Reset()
    {
        last = null;
    }

    public async Task UpdateAsync()
    {
        if (Interval <= 0)
            return;

        if (SuccessUrl == string.Empty)
            return;

        var now = DateTime.Now;
        if (last is not null && (now - last.Value).TotalSeconds < Interval)
            return;

        last = now;

        if ((bool) checkMethods[CheckType].Invoke(this, null))
            return;

        using var httpClient = new HttpClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, SuccessUrl);
        foreach (var header in SuccessHeaders)
            request.Headers.Add(header.Value, header.Description);

        try
        {
            await httpClient.SendAsync(request);
        }
        catch
        {
        }
    }
}
