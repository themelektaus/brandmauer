using Microsoft.AspNetCore.Mvc;

using System.Security.Cryptography.X509Certificates;

namespace Brandmauer;

public static partial class Endpoint
{
    public static class Certificates
    {
        static readonly string exportedCertificatesFolder = Path.Combine("Data", "Exported Certificates");

        public static IResult GetAll()
        {
            var data = Certificate.GetAll();

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Certificate.Get(id);

            if (data is null)
                return Results.NotFound();

            return Results.Json(data);
        }

        public static IResult Post()
        {
            var data = Database.Use(x =>
            {
                var newData = x.Create<Certificate>();
                x.Certificates.Add(newData);
                x.Save();
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(Certificate data)
        {
            Database.Use(x =>
            {
                var index = x.Certificates.FindIndex(x => x.Identifier.Id == data.Identifier.Id);
                x.Certificates[index] = x.Replace(x.Certificates[index], data);
                x.Save();
            });

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.Certificates.RemoveAll(x =>
                {
                    x.Dispose();
                    return x.Identifier.Id == id;
                });
                x.Save();
            });
            return Results.Ok();
        }

        public static IResult ExportAll()
        {
            ClearAllExports();

            foreach (var certificate in Database.Use(x => x.Certificates))
                Export(certificate);

            return Results.Ok();
        }

        public static IResult ClearAllExports()
        {
            if (Directory.Exists(exportedCertificatesFolder))
                Directory.Delete(exportedCertificatesFolder, true);

            return Results.Ok();
        }

        static void Export(Certificate certificate)
        {
            Directory.CreateDirectory(exportedCertificatesFolder);

            var name = $"{certificate.Identifier.Id} {certificate.Name}";

            File.WriteAllText(
                Path.Combine(exportedCertificatesFolder, $"{name}.crt"),
                certificate.CertPem
            );

            File.WriteAllText(
                Path.Combine(exportedCertificatesFolder, $"{name}.key"),
                certificate.KeyPem
            );

            File.WriteAllBytes(
                Path.Combine(exportedCertificatesFolder, $"{name}.pfx"),
                certificate.PfxData
            );
        }

        public static async Task<IResult> Update(long id, bool? letsEncrypt = false, bool? staging = true)
        {
            var certificate = Certificate.Get(id);
            if (certificate is null)
                return Results.NotFound();

            var domains = certificate.Domains.Select(x => x.Value).ToArray();

            X509Certificate2 pfxCert;

            if (letsEncrypt ?? false)
            {
                var accountMailAddress = Database.Use(x => x.Config.LetsEncryptAccountMailAddress);
                
                pfxCert = await CertificateUtils.RequestLetsEncryptAsync(accountMailAddress, staging ?? true, domains);
                if (pfxCert is null)
                    return Results.StatusCode(500);
            }
            else
            {
                var ca = Database.Use(x => x.Certificates.FirstOrDefault(x => x.HasAuthority));
            
                pfxCert = CertificateUtils.CreateSelfSigned(ca, domains);
                if (pfxCert is null)
                    return Results.StatusCode(500);
            }

            Database.Use(x =>
            {
                certificate.Write(x, pfxCert);
                x.Save();
            });

            return Results.Ok();
        }

        public static IResult Download(long id, [FromQuery] string format)
        {
            var certificate = Certificate.Get(id);

            var data = format switch
            {
                "crt" => certificate.CertPem.ToBytes(),
                "key" => certificate.KeyPem.ToBytes(),
                "pfx" => certificate.PfxData,
                _ => null
            };

            if (data is null)
                return Results.BadRequest();

            return Results.File(
                data,
                "application/octet-stream",
                $"{certificate.Name}.{format}"
            );
        }
    }
}
