namespace WAMS.Api.Security;

public static class SessionVersionClaim
{
    public static bool TryParse(string? claimValue, out int version)
    {
        if (claimValue is null)
        {
            version = 0;
            return true;
        }

        return int.TryParse(claimValue, out version);
    }
}
