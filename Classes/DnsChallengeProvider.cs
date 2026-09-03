namespace Brandmauer;

public class DnsChallengeProvider(DynamicDnsHost host)
{
    public const string RecordPrefix = "_acme-challenge";

    readonly List<int> createdRecordIds = new();

    public string Zone { get; } = host.GetDnsChallengeZone();

    readonly string authorization = host.GetDnsChallengeAuthorization();

    public string HostName => host.Name;

    public bool IsSupported
        => !string.IsNullOrEmpty(Zone)
        && !string.IsNullOrEmpty(authorization);

    public static string GetRecordName(string identifier)
        => $"{RecordPrefix}.{identifier.TrimStart('*', '.')}";

    public static string GetRelativeHost(string zone, string recordName)
    {
        if (string.IsNullOrEmpty(zone) || string.IsNullOrEmpty(recordName))
            return null;

        if (recordName == zone)
            return string.Empty;

        if (!recordName.EndsWith($".{zone}"))
            return null;

        return recordName[..^(zone.Length + 1)];
    }

    public async Task<bool> AddAsync(string recordName, string value)
    {
        var relative = GetRelativeHost(Zone, recordName);

        if (relative is null)
        {
            Audit.Error<DnsChallengeProvider>(
                $"\"{recordName}\" is not inside the zone \"{Zone}\" of the "
                    + $"Dynamic DNS entry \"{HostName}\"."
            );
            return false;
        }

        var id = await Endpoint.NameCom.CreateTxtRecordAsync(
            authorization,
            Zone,
            relative,
            value
        );

        if (!id.HasValue)
            return false;

        createdRecordIds.Add(id.Value);
        return true;
    }

    public async Task CleanupAsync()
    {
        foreach (var id in createdRecordIds)
        {
            try
            {
                await Endpoint.NameCom.DeleteRecordAsync(
                    authorization,
                    Zone,
                    id
                );
            }
            catch (Exception ex)
            {
                Audit.Error<DnsChallengeProvider>(ex);
            }
        }

        createdRecordIds.Clear();
    }
}
