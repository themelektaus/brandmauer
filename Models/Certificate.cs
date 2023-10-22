using System.Security.Cryptography.X509Certificates;

using System.Text;
using System.Text.Json.Serialization;

namespace Brandmauer;

public class Certificate : Model, IOnDeserialize
{
    class Cache : ThreadsafeCache<string, Certificate>
    {
        protected override bool Logging => false;

        public Database database;

        protected override Certificate GetNew(string key)
        {
            return database.Certificates
                .FirstOrDefault(x
                    => x.Enabled
                    && x.isValid
                    && !x.HasAuthority
                    && (
                        x.Domains.Select(y => y.Value).Contains(key) ||
                        x.Domains.Any(y => y.Value.StartsWith('*') && key.EndsWith(y.Value[1..]))
                    )
                );
        }
    }
    static readonly Cache cache = new();

    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            builder.Append("<div class=\"badges\">");
            foreach (var domain in Domains)
                builder.AppendBadge("certificate", "domain", domain, null);
            builder.Append("</div>");

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
    public bool isLetsEncrypt;
    public bool isLetsEncryptStaging;

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

        isLetsEncrypt = false;
        isLetsEncryptStaging = false;

        if (Pem.IssuerName.ToDictonary().TryGetValue("O", out var o))
        {
            if (o == "(STAGING) Let's Encrypt")
                isLetsEncryptStaging = true;

            if (isLetsEncryptStaging || o == "Let's Encrypt")
                isLetsEncrypt = true;
        }
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
}
