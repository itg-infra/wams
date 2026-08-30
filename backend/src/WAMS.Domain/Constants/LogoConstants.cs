namespace WAMS.Domain.Constants;

public static class LogoConstants
{
    public const long MaxSizeBytes = 2 * 1024 * 1024;
    public static readonly HashSet<string> AllowedContentTypes = ["image/png", "image/jpeg", "image/webp"];
}
