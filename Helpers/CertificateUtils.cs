// Source: https://github.com/rlipscombe/bouncy-castle-csharp/blob/master/CreateCertificate/Program.cs

extern alias PortableBouncyCastle;

using Certes;
using Certes.Acme;
using Certes.Acme.Resource;

using PortableBouncyCastle.Org.BouncyCastle.Asn1;
using PortableBouncyCastle.Org.BouncyCastle.Asn1.X509;
using PortableBouncyCastle.Org.BouncyCastle.Crypto;
using PortableBouncyCastle.Org.BouncyCastle.Crypto.Generators;
using PortableBouncyCastle.Org.BouncyCastle.Crypto.Prng;
using PortableBouncyCastle.Org.BouncyCastle.Math;
using PortableBouncyCastle.Org.BouncyCastle.Pkcs;
using PortableBouncyCastle.Org.BouncyCastle.Security;
using PortableBouncyCastle.Org.BouncyCastle.Utilities;
using PortableBouncyCastle.Org.BouncyCastle.X509;

using RSAExtensions
    = System.Security.Cryptography.X509Certificates.RSACertificateExtensions;

using X509Certificate2
    = System.Security.Cryptography.X509Certificates.X509Certificate2;

using X509KeyStorageFlags
    = System.Security.Cryptography.X509Certificates.X509KeyStorageFlags;

using RegionInfo = System.Globalization.RegionInfo;

namespace Brandmauer;

public static class CertificateUtils
{
    public static readonly ThreadsafeObject<Dictionary<string, string>>
        acmeChallenges = new(new());

    public static X509Certificate2 CreateSelfSigned(
        Certificate ca,
        params string[] domains
    )
    {
        if (ca is null)
            return null;

        var startDate = DateTime.UtcNow;

        return CreateCertificate(
            $"CN={domains[0]}",
            startDate,
            startDate.AddYears(1),
            ca.Pem,
            domains
        );
    }

    public static async Task<X509Certificate2> RequestLetsEncryptAsync(
        string accountMailAddress,
        bool staging,
        params string[] domains
    )
    {
        if (domains.Length == 0)
            return null;

        var accountKey = KeyFactory.NewKey(KeyAlgorithm.ES256);

        var acmeContext = new AcmeContext(
            staging
                ? WellKnownServers.LetsEncryptStagingV2
                : WellKnownServers.LetsEncryptV2,
            accountKey
        );

        Audit.Info(
            "ACME",
            $"Creating new account: \"{accountMailAddress}\"{(
                staging ? " (STAGING)" : ""
            )}"
        );

        await acmeContext.NewAccount(accountMailAddress, true);

        var orderContext = await acmeContext.NewOrder(domains);

        foreach (var authContext in await orderContext.Authorizations())
        {
            var challengeContext = await authContext.Http();

            var token = challengeContext.Token;
            Audit.Info("ACME", $"Token: {token}");

            var keyAuthz = challengeContext.KeyAuthz;
            Audit.Info("ACME", $"KeyAuthz: {keyAuthz}");
            
            acmeChallenges.Use(x => x.Add(token, keyAuthz));

            var challenge = await challengeContext.Validate();

            for (; ; )
            {
                await Task.Delay(500);

                challenge = await challengeContext.Resource();
                if (challenge.Status == ChallengeStatus.Valid)
                    break;

                if (challenge.Status == ChallengeStatus.Invalid)
                {
                    Audit.Error("ACME", challenge.Error.Detail);
                    return null;
                }

                await Task.Delay(500);
            }
            Audit.Info("ACME", $"Challenge Status: {challenge.Status}");
        }

        var certKey = KeyFactory.NewKey(KeyAlgorithm.RS256);

        var order = await orderContext.Finalize(
            new CsrInfo
            {
                CountryName = RegionInfo.CurrentRegion.TwoLetterISORegionName,
                Organization = Utils.Name,
            },
            certKey
        );

        for (; ; )
        {
            await Task.Delay(500);

            order = await orderContext.Resource();
            if (order.Status == OrderStatus.Valid)
                break;

            if (order.Status == OrderStatus.Invalid)
            {
                Audit.Error("ACME", order.Error);
                return null;
            }

            await Task.Delay(500);
        }

        Audit.Info("ACME", $"Order Status: {order.Status}");
        
        var cert = await orderContext.Download();

        var pfx = cert.ToPfx(certKey);

        if (staging)
        {
            var caFileName = "letsencrypt-stg-root-x1.pem";
            var caFile = Path.Combine("Data", caFileName);

            if (!File.Exists(caFile))
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(
                    $"https://letsencrypt.org/certs/staging/{caFileName}"
                );
                var bytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(caFile, bytes);
            }

            pfx.AddIssuer(await File.ReadAllBytesAsync(caFile));
        }

        var pfxData = pfx.Build(domains[0], string.Empty);
        return new(pfxData, string.Empty, X509KeyStorageFlags.Exportable);
    }

    public static X509Certificate2 CreateCertificate(
        string subjectName,
        DateTime startDate,
        DateTime endDate,
        X509Certificate2 issuerCertificate = null,
        string[] subjectAlternativeNames = null
    )
    {
        var random = new SecureRandom(new CryptoApiRandomGenerator());
        var subjectKeyPair = GenerateKeyPair(random, 2048);
        var serialNumber = BigIntegers.CreateRandomInRange(
            BigInteger.One,
            BigInteger.ValueOf(long.MaxValue),
            random
        );

        AsymmetricCipherKeyPair issuerKeyPair;

        if (issuerCertificate is null)
        {
            issuerKeyPair = subjectKeyPair;
        }
        else
        {
            var key = RSAExtensions.GetRSAPrivateKey(issuerCertificate);
            issuerKeyPair = DotNetUtilities.GetKeyPair(key);
        }

        var certificate = GenerateCertificate(
            random,
            subjectName,
            subjectKeyPair,
            serialNumber,
            subjectAlternativeNames,
            issuerCertificate?.Subject ?? subjectName,
            issuerKeyPair,
            issuerCertificate is null
                ? serialNumber
                : new BigInteger(issuerCertificate.GetSerialNumber()),
            issuerCertificate is null,
            startDate,
            endDate,
            issuerCertificate is null
                ? [KeyPurposeID.AnyExtendedKeyUsage]
                : null
        );
        return ConvertCertificate(certificate, subjectKeyPair, random);
    }

    static X509Certificate GenerateCertificate(
        SecureRandom random,
        string subjectName,
        AsymmetricCipherKeyPair subjectKeyPair,
        BigInteger subjectSerialNumber,
        string[] subjectAlternativeNames,
        string issuerName,
        AsymmetricCipherKeyPair issuerKeyPair,
        BigInteger issuerSerialNumber,
        bool isCertificateAuthority,
        DateTime startDate,
        DateTime endDate,
        KeyPurposeID[] usages
    )
    {
        var generator = new X509V3CertificateGenerator();

        generator.SetSerialNumber(subjectSerialNumber);
#pragma warning disable CS0618
        generator.SetSignatureAlgorithm("SHA256WithRSA");
#pragma warning restore CS0618

        var issuerDN = new X509Name(issuerName);
        generator.SetIssuerDN(issuerDN);

        var subjectDN = new X509Name(subjectName);
        generator.SetSubjectDN(subjectDN);

        generator.SetNotBefore(startDate);
        generator.SetNotAfter(endDate);

        generator.SetPublicKey(subjectKeyPair.Public);

        AddAuthorityKeyIdentifier(
            generator,
            issuerDN,
            issuerKeyPair,
            issuerSerialNumber
        );
        AddSubjectKeyIdentifier(generator, subjectKeyPair);
        AddBasicConstraints(generator, isCertificateAuthority);

        if (usages?.Length > 0)
            AddExtendedKeyUsage(generator, usages);

        if (subjectAlternativeNames?.Length > 0)
            AddSubjectAlternativeNames(generator, subjectAlternativeNames);

#pragma warning disable CS0618
        return generator.Generate(issuerKeyPair.Private, random);
#pragma warning restore CS0618
    }

    static AsymmetricCipherKeyPair GenerateKeyPair(
        SecureRandom random,
        int strength
    )
    {
        var parameters = new KeyGenerationParameters(random, strength);
        var generator = new RsaKeyPairGenerator();
        generator.Init(parameters);
        return generator.GenerateKeyPair();
    }

    static void AddAuthorityKeyIdentifier(
        X509V3CertificateGenerator generator,
        X509Name issuerDN,
        AsymmetricCipherKeyPair issuerKeyPair,
        BigInteger issuerSerialNumber
    )
    {
        generator.AddExtension(
            oid: X509Extensions.AuthorityKeyIdentifier.Id,
            critical: false,
            extensionValue: new AuthorityKeyIdentifier(
                SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(
                    issuerKeyPair.Public
                ),
                new GeneralNames(new GeneralName(issuerDN)),
                issuerSerialNumber
            )
        );
    }

    static void AddSubjectAlternativeNames(
        X509V3CertificateGenerator generator,
        IEnumerable<string> subjectAlternativeNames
    )
    {
        generator.AddExtension(
            oid: X509Extensions.SubjectAlternativeName.Id,
            critical: false,
            extensionValue: new DerSequence(
                subjectAlternativeNames
                    .Select(name => new GeneralName(GeneralName.DnsName, name))
                    .ToArray<Asn1Encodable>()
            )
        );
    }

    static void AddExtendedKeyUsage(
        X509V3CertificateGenerator generator,
        KeyPurposeID[] usages
    )
    {
        generator.AddExtension(
            oid: X509Extensions.ExtendedKeyUsage.Id,
            critical: false,
            extensionValue: new ExtendedKeyUsage(usages)
        );
    }

    static void AddBasicConstraints(
        X509V3CertificateGenerator generator,
        bool isCertificateAuthority
    )
    {
        generator.AddExtension(
            oid: X509Extensions.BasicConstraints.Id,
            critical: true,
            extensionValue: new BasicConstraints(
                isCertificateAuthority
            )
        );
    }

    static void AddSubjectKeyIdentifier(
        X509V3CertificateGenerator generator,
        AsymmetricCipherKeyPair subjectKeyPair
    )
    {
        generator.AddExtension(
            oid: X509Extensions.SubjectKeyIdentifier.Id,
            critical: false,
            extensionValue: new SubjectKeyIdentifier(
                SubjectPublicKeyInfoFactory
                    .CreateSubjectPublicKeyInfo(subjectKeyPair.Public)
            )
        );
    }

    static X509Certificate2 ConvertCertificate(
        X509Certificate certificate,
        AsymmetricCipherKeyPair subjectKeyPair,
        SecureRandom random
    )
    {
        var store = new Pkcs12Store();
        var friendlyName = certificate.SubjectDN.ToString();
        var certificateEntry = new X509CertificateEntry(certificate);
        store.SetCertificateEntry(friendlyName, certificateEntry);
        store.SetKeyEntry(
            friendlyName,
            new AsymmetricKeyEntry(subjectKeyPair.Private),
            [certificateEntry]
        );
        using var stream = new MemoryStream();
        store.Save(stream, [], random);
        return new X509Certificate2(
            rawData: stream.ToArray(),
            password: string.Empty,
            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable
        );
    }
}
