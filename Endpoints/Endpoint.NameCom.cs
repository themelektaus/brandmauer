namespace Brandmauer;

using _NameCom = NameCom;
using _Audit = Audit;

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
            using var response = await httpClient.TrySendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _Audit.Error(
                    "name.com",
                    $"GET /domains{path} -> {(int) response.StatusCode}"
                );
                return default;
            }

            return await response.Content.ReadFromJsonAsync<_NameCom>();
        }

        public static async Task<int?> CreateTxtRecordAsync(
            string authorization,
            string domain,
            string host,
            string answer
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://api.name.com/v4/domains/{domain}/records"
            );

            request.Headers.Add("Authorization", authorization);

            request.Content = JsonContent.Create(new
            {
                host,
                type = "TXT",
                answer,
                ttl = 300
            });

            using var httpClient = new HttpClient();
            using var response = await httpClient.TrySendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _Audit.Error(
                    "name.com",
                    $"Creating TXT record \"{host}.{domain}\" failed with "
                        + $"{(int) response.StatusCode}: {body}"
                );
                return null;
            }

            var record = await response.Content
                .ReadFromJsonAsync<_NameCom.Record>();

            domainRecordsCache.Clear();

            _Audit.Info(
                "name.com",
                $"Created TXT record \"{host}.{domain}\" (id {record.Id})"
            );

            return record.Id;
        }

        public static async Task<bool> DeleteRecordAsync(
            string authorization,
            string domain,
            int id
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"https://api.name.com/v4/domains/{domain}/records/{id}"
            );

            request.Headers.Add("Authorization", authorization);

            using var httpClient = new HttpClient();
            using var response = await httpClient.TrySendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _Audit.Error(
                    "name.com",
                    $"Deleting record {id} of \"{domain}\" failed with "
                        + $"{(int) response.StatusCode}: {body}"
                );
                return false;
            }

            domainRecordsCache.Clear();

            _Audit.Info("name.com", $"Deleted record {id} of \"{domain}\"");

            return true;
        }
    }
}
