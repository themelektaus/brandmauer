using OtpNet;

using QRCoder;

namespace Brandmauer;

public static class TotpUtils
{
    public static string GenerateUrl(string user, string secret)

        => new OtpUri(

            schema: OtpType.Totp,
            secret: secret,
            user: user ?? string.Empty,
            issuer: Utils.Name,
            algorithm: OtpHashMode.Sha1,
            digits: 6,
            period: 30

        ).ToString();

    public static string ComputeCode(string secret, int offset)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.ComputeTotp(DateTime.UtcNow.AddSeconds(offset * 30));
    }

    public static byte[] GenerateQrCode(string text)
    {
        return PngByteQRCodeHelper.GetQRCode(
            text, QRCodeGenerator.ECCLevel.M, 6
        );
    }
}
