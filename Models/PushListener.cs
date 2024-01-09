namespace Brandmauer;

public class PushListener : Model, IOnDeserialize
{
    public bool Enabled { get; set; } = true;

    public string Token { get; set; } = Utils.GenerateToken();
    public long AgeThreshold { get; set; } = 120;

    public Identifier SmtpConnectionReference { get; set; }
    public SmtpConnection SmtpConnection;

    public List<StringValue> Receivers { get; set; } = new();

    public DateTime LastTouch { get; set; }
    public string LastTouchString;

    public string url;
    public string header;

    public void Touch()
    {
        LastTouch = DateTime.Now;
    }

    public async Task UpdateAsync()
    {
        var future = LastTouch.AddSeconds(AgeThreshold);
        if (future >= DateTime.Now)
            return;

        Database.Use(x =>
        {
            Touch();
            x.Save(logging: false);
        });

        if (SmtpConnection is null || Receivers.Count == 0)
            return;

        var receivers = Receivers.Select(x => x.ToString()).ToList();

        var (statusCode, text) = await SmtpConnection
            .SendAsync(
                receivers,
                subject: $"🔥 Push Listener: {Name}",
                body: $"The Push Listener \"{Name}\" has not been invoked "
                    + $"for over {AgeThreshold} seconds.",
                html: false
            );

        Utils.Log(nameof(IntervalTask_Push), $"{statusCode}: {text}");
    }

    public void OnDeserialize(Database database)
    {
        SmtpConnection = database.SmtpConnections.FirstOrDefault(
            x => x.Identifier.Id == SmtpConnectionReference.Id
        );

        LastTouchString = LastTouch.ToHumanizedString();

        url = $"{database.GetBaseUrl()}/push";
        header = $"X-{Utils.Name}-Token: {Token}";
    }
}
