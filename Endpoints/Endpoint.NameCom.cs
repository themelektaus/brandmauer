namespace Brandmauer;

using _NameCom = NameCom;

public static partial class Endpoint
{
    public static class NameCom
    {
        class DomainsCache : AsyncThreadsafeCache<string, List<string>>
        {
            protected override bool Logging => true;

            protected override TimeSpan? MaxAge => TimeSpan.FromMinutes(15);

            protected override async Task<List<string>> GetNewAsync(string key)
            {
                var nameCom = await FetchAsync(key);
                var domains = nameCom.Domains ?? new();
                return domains.Select(x => x.DomainName).ToList();
            }
        }
        static readonly DomainsCache domainsCache = new();

        public static async Task<IResult> GetDomains(HttpContext context)
        {
            var headers = context.Request.Headers;
            var authorization = headers.Authorization.FirstOrDefault();
            var domains = await domainsCache.GetAsync(authorization);
            return Results.Json(domains);
        }

        class DomainRecordsCache : AsyncThreadsafeCache<
            (string authorization, string domain),
            List<_NameCom.Record>
        >
        {
            protected override bool Logging => true;

            protected override TimeSpan? MaxAge => TimeSpan.FromMinutes(15);

            protected override async Task<List<_NameCom.Record>> GetNewAsync(
                (string authorization, string domain) key
            )
            {
                var nameCom = await FetchAsync(
                    key.authorization,
                    $"/{key.domain}/records"
                );
                return nameCom.Records;
            }
        }
        static readonly DomainRecordsCache domainRecordsCache = new();

        public static async Task<IResult> GetDomainRecords(
            HttpContext context,
            string domain
        )
        {
            var headers = context.Request.Headers;
            var authorization = headers.Authorization.FirstOrDefault();
            var key = (authorization, domain);
            var records = await domainRecordsCache.GetAsync(key);
            return Results.Json(records);
        }

        static async Task<_NameCom> FetchAsync(
            string authorization,
            string path = ""
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.name.com/v4/domains{path}"
            );

            request.Headers.Add("Authorization", authorization);

            using var httpClient = new HttpClient();
            using var response = await httpClient.SendAsync(request);

#if DEBUG
            await Task.Delay(TimeSpan.FromSeconds(3));
#endif

            return await response.Content.ReadFromJsonAsync<_NameCom>();
        }
    }
}
