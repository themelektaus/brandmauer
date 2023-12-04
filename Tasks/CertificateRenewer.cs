namespace Brandmauer;

public class CertificateRenewer : ConditionalIntervalTask
{
    protected override TimeSpan Delay => TimeSpan.FromMinutes(1);
    protected override TimeSpan Interval => TimeSpan.FromDays(1);

    protected override bool ShouldTrigger()
    {
        return true;
    }

    protected override Task OnDisposeAsync() => Task.CompletedTask;

    protected override async Task OnTriggerAsync()
    {
        var certificates = Certificate.GetAll()
            .Where(x => x.ExpiresSoon).ToList();

        var letsEncryptCertificate = certificates.FirstOrDefault(
            x => x.issuerCommonName == "R3" &&
                x.issuerOrganisation == "Let's Encrypt"
        );

        if (letsEncryptCertificate is not null)
        {
            await Endpoint.Certificates.Update(
                id: letsEncryptCertificate.Identifier.Id,
                letsEncrypt: true
            );
        }

        foreach (var certificate in certificates
            .Where(x => !x.issuerCommonName.EndsWith("R3"))
        )
        {
            await Endpoint.Certificates.Update(
                id: certificate.Identifier.Id,
                letsEncrypt: false
            );
        }
    }
}
