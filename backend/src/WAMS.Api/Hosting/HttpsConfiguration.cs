namespace WAMS.Api.Hosting;

public sealed record HttpsConfiguration(
    bool Enabled,
    int Port,
    string? CertificatePath,
    string? CertificatePassword)
{
    public static HttpsConfiguration Read(IConfiguration configuration)
    {
        var enabled = configuration.GetValue("HTTPS", false);
        var port = configuration.GetValue("PORT", 8080);

        if (!enabled)
            return new HttpsConfiguration(false, port, null, null);

        var certificatePath = configuration["HTTPS_CERT_PATH"];
        if (string.IsNullOrWhiteSpace(certificatePath))
            throw new InvalidOperationException("HTTPS_CERT_PATH is required when HTTPS=true");

        var certificatePassword = configuration["HTTPS_CERT_PASSWORD"];

        if (string.IsNullOrWhiteSpace(certificatePassword))
            throw new InvalidOperationException("HTTPS_CERT_PASSWORD is required when HTTPS=true");

        return new HttpsConfiguration(true, port, certificatePath, certificatePassword);
    }
}
