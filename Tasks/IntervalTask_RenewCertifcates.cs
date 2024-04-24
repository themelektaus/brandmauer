namespace Brandmauer;

[Delay(60)]
[Interval(86400)]
public class IntervalTask_RenewCertifcates : IntervalTask
{
    protected override Task OnStartAsync() => default;

    protected override Task OnBeforeFirstTickAsync() => default;

    protected override async Task OnTickAsync()
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
                letsEncrypt: true,
                staging: false
            );
        }

        foreach (
            var certificate in certificates.Where(
                x => x.issuerOrganisation == string.Empty
            )
        )
        {
            await Endpoint.Certificates.Update(
                id: certificate.Identifier.Id,
                letsEncrypt: false,
                staging: false
            );
        }
    }

    protected override Task OnDisposeAsync() => default;
}
