using System.Security.Cryptography.X509Certificates;

using System.Text;
using System.Text.Json.Serialization;

namespace Brandmauer;

public class Certificate : Model, IOnDeserialize
{
    class Cache : ThreadsafeCache<string, Certificate>
    {
        protected override bool Logging => false;

        protected override TimeSpan? MaxAge => null;

        public Database database;

        protected override Certificate GetNew(
            Dictionary<string, TempValue> _,
            string key
        )
        {
            return database.Certificates
                .Where(x
                    => x.Enabled
                    && x.isValid
                    && !x.HasAuthority
                    && (
                        x.Domains.Select(y => y.Value).Contains(key) ||
                        x.Domains.Any(y
                            => y.Value.StartsWith('*')
                            && key.EndsWith(y.Value[1..])
                        )
                    )
                )
                .OrderByDescending(x => x.Domains.Any(y => y.Value == key))
                .FirstOrDefault();
        }
    }
    static readonly Cache cache = new();

    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            builder.BeginBadges("flex: 2; ");
            {
                if (Name != string.Empty)
                    builder.AppendBadge("certificate", "name", "Name", Name);

                foreach (var domain in Domains)
                    builder.AppendBadge("certificate", "domain", domain, null);
            }
            builder.EndBadges();

            builder.BeginBadges("flex: 1; ");
            {
                if (isValid)
                    builder.AppendBadge("certificate", "valid", "Valid", "<i class=\"fa-solid fa-check\"></i>");
                else
                    builder.AppendBadge("certificate", "invalid", "Invalid", "<i class=\"fa-solid fa-xmark\"></i>");

                if (!string.IsNullOrEmpty(expiresIn))
                    builder.AppendBadge("certificate", "expires-in", "Expires in", expiresIn);

                if (!string.IsNullOrEmpty(issuerCommonName))
                    builder.AppendBadge("certificate", "issuer", "CN", issuerCommonName);

                if (!string.IsNullOrEmpty(issuerOrganisation))
                    builder.AppendBadge("certificate", "organisation", "O", issuerOrganisation);

            }
            builder.EndBadges();

            return builder.ToString();
        }
    }

    public bool Enabled { get; set; } = true;

    public List<StringValue> Domains { get; set; } = new();
    public string CertPem { get; set; }
    public string KeyPem { get; set; }
    public bool HasAuthority { get; set; }

    [JsonIgnore] public X509Certificate2 Pem { get; private set; }
    [JsonIgnore] public byte[] PfxData { get; private set; }
    [JsonIgnore] public X509Certificate2 Pfx { get; private set; }

    public string fileBaseName;
    public bool isValid;
    public DateTime startDate;
    public DateTime endDate;
    public int daysUntilExpiry;
    public string expiresIn;
    public string issuerCommonName;
    public string issuerOrganisation;

    [JsonIgnore] public bool ExpiresSoon => daysUntilExpiry <= 20;

    public void Write(Database database, X509Certificate2 pfxCert)
    {
        CertPem = pfxCert.ExportCertificatePem();
        KeyPem = pfxCert.GetRSAPrivateKey().ExportRSAPrivateKeyPem();
        OnDeserialize(database);
        cache.Clear();
    }

    public void OnDeserialize(Database database)
    {
        if (CertPem is null)
            return;

        if (KeyPem is null)
            return;

        Pem = X509Certificate2.CreateFromPem(CertPem, KeyPem);
        PfxData = Pem.Export(X509ContentType.Pfx);
        Pfx = new(PfxData, string.Empty, X509KeyStorageFlags.Exportable);

        if (Pem.SubjectName.ToDictonary().TryGetValue("CN", out var cn))
            fileBaseName = cn;

        var now = DateTime.UtcNow;
        var start = Pem.NotBefore;
        var end = Pem.NotAfter;

        isValid = start.AddDays(-1) <= now && now < end;
        startDate = start;
        endDate = end;

        var expiresIn = endDate - now.AddDays(1);
        daysUntilExpiry = (int) expiresIn.TotalDays;
        this.expiresIn = expiresIn.ToHumanReadableDaysString();

        if (!Pem.IssuerName.ToDictonary().TryGetValue("CN", out issuerCommonName))
            issuerCommonName = string.Empty;

        if (!Pem.IssuerName.ToDictonary().TryGetValue("O", out issuerOrganisation))
            issuerOrganisation = string.Empty;
    }

    public static List<Certificate> GetAll()
    {
        var certificates = Database.Use(x => x.Certificates.ToList());

        return certificates;
    }

    public static Certificate Get(long id)
    {
        return Database.Use(
            x => x.Certificates.FirstOrDefault(
                x => x.Identifier.Id == id
            )
        );
    }

    public static Certificate Get(string domain)
    {
        return Database.Use(database =>
        {
            cache.database = database;
            return cache.Get(domain);
        });
    }

    public string GetFilename(string format)
    {
        if (!string.IsNullOrEmpty(Name))
            return $"{Name}.{format}";

        var domain = Domains.FirstOrDefault().Value;
        if (!string.IsNullOrEmpty(domain))
            return $"{domain}.{format}";

        return $"{Identifier.Id}.{format}";
    }
}
