namespace WAMS.Infrastructure.Services.Common;

using WAMS.Application.Interfaces.Common;

public sealed class TenantContext : ITenantContext
{
    private long? _companyId;
    private bool _isSet;

    public long? CompanyId => _isSet ? _companyId : null;

    public bool IsSet => _isSet;

    public void SetCompanyId(long companyId)
    {
        if (companyId <= 0)
            throw new ArgumentException("CompanyId must be a positive number");

        _companyId = companyId;
        _isSet = true;
    }
}
