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

        certificatePath = ResolveCertificatePath(certificatePath);

        return new HttpsConfiguration(true, port, certificatePath, certificatePassword);
    }

    private static string ResolveCertificatePath(string path)
    {
        if (File.Exists(path))
            return path;
        if (!Directory.Exists(path))
            throw new InvalidOperationException($"HTTPS certificate path does not exist: {path}");

        var certificates = Directory.EnumerateFiles(path)
            .Where(file => string.Equals(Path.GetExtension(file), ".pfx", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return certificates.Length switch
        {
            0 => throw new InvalidOperationException($"HTTPS certificate directory contains no .pfx files: {path}"),
            1 => certificates[0],
            _ => throw new InvalidOperationException(
                $"HTTPS certificate directory contains {certificates.Length} .pfx files: {path}. " +
                "Set HTTPS_CERT_PATH to the exact .pfx file to use.")
        };
    }
}
