namespace WAMS.Application.Interfaces.Common;

public interface ITenantContext
{
    long? CompanyId { get; }

    /// <summary>
    /// True when the context has been explicitly set by the middleware.
    /// </summary>
    bool IsSet { get; }

    void SetCompanyId(long companyId);
}
