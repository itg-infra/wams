namespace WAMS.Application.Services.Companies;

using Microsoft.Extensions.Logging;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Companies;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Files;
using WAMS.Application.Interfaces.Users;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Constants;
using WAMS.Domain.Exceptions;

public sealed class CompanyService(
    ICompanyRepository companyRepo,
    IUserRepository userRepo,
    ILogger<CompanyService> logger,
    IUnitOfWork uow,
    ITenantContext tenantContext,
    IFileAttachmentStorage storage,
    IFileMimeDetector mimeDetector) : ICompanyService
{

    private readonly ICompanyRepository _companyRepo = companyRepo;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly ILogger<CompanyService> _logger = logger;
    private readonly IUnitOfWork _uow = uow;
    private readonly ITenantContext _tenantContext = tenantContext;
    private readonly IFileAttachmentStorage _storage = storage;
    private readonly IFileMimeDetector _mimeDetector = mimeDetector;

    public async Task<CompanyResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _companyRepo.GetByIdWithCountsAsync(id, ct)
            ?? throw new NotFoundException("Company", id);
    }

    public async Task<PaginatedResponse<CompanyResponse>> GetAllAsync(
        DataTableQuery query,
        CancellationToken ct = default
    )
    {
        var (items, total) = await _companyRepo.GetAllAsync(query, ct);
        var totalPages = (int)Math.Ceiling((double)total / query.Limit);

        return new PaginatedResponse<CompanyResponse>(
            true,
            items,
            new PaginationMeta(query.Page, query.Limit, total, totalPages)
        );
    }

    public IAsyncEnumerable<CompanyResponse> StreamAllAsync(
        DataTableQuery query,
        int limit,
        CancellationToken ct = default
    ) => _companyRepo.StreamAllAsync(query, limit, ct);

    public async Task<List<CompanyPublicResponse>> GetActivePublicAsync(
        string? code = null,
        CancellationToken ct = default
    )
    {
        var companies = await _companyRepo.GetActiveAsync(code, ct);

        return [.. companies.Select(c => new CompanyPublicResponse(c.Id, c.Code, c.Name))];
    }

    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default)
    {
        // Check for duplicate code
        if (await _companyRepo.CodeExistsAsync(request.Code, ct))
            throw new ConflictException(ErrorMessages.Company.CodeConflict(request.Code));

        var company = new Company
        {
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            IsActive = true
        };

        await _companyRepo.CreateAsync(company, ct);
        await _uow.CommitAsync(ct);

        _logger.LogInformation("Company created: {Code} - {Name}", company.Code, company.Name);

        return ToResponse(company);
    }

    public async Task<CompanyResponse> UpdateAsync(
        long id,
        UpdateCompanyRequest request,
        CancellationToken ct = default
    )
    {
        var company = await _companyRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Company", id);

        if (request.Name != null) company.Name = request.Name;
        if (request.Address != null) company.Address = request.Address;
        if (request.Phone != null) company.Phone = request.Phone;
        if (request.Email != null) company.Email = request.Email;
        if (request.IsActive.HasValue) company.IsActive = request.IsActive.Value;

        await _companyRepo.UpdateAsync(company, ct);
        await _uow.CommitAsync(ct);

        _logger.LogInformation("Company updated: {Id} - {Code}", company.Id, company.Code);

        return ToResponse(company);
    }

    public async Task DeactivateAsync(long id, CancellationToken ct = default)
    {
        var company = await _companyRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Company", id);

        if (company.Code == CompanyCodes.Default)
            throw new ForbiddenException(ErrorMessages.Company.CannotDeactivateDefault);

        company.IsActive = false;

        await _companyRepo.UpdateAsync(company, ct);
        await _uow.CommitAsync(ct);

        _logger.LogInformation("Company deactivated: {Id} - {Code}", company.Id, company.Code);
    }

    public async Task AssignUserToCompanyAsync(
        long userId,
        long companyId,
        CancellationToken ct = default
    )
    {
        // Verify company exists
        if (!await _companyRepo.ExistsAsync(companyId, ct))
            throw new NotFoundException("Company", companyId);

        // Get user (need to bypass tenant filter to find users in other companies)
        var user = await _userRepo.GetByIdUnfilteredAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (user.CompanyId == companyId)
            throw new ConflictException(ErrorMessages.Company.AlreadyAssigned);

        var oldCompanyId = user.CompanyId;
        user.CompanyId = companyId;

        // Also clear warehouse assignments since they belong to the old company
        // This prevents a user from having warehouses from a different company
        await _userRepo.ClearWarehouseAssignmentsAsync(userId, ct);
        await _userRepo.UpdateAsync(user, ct);
        await _uow.CommitAsync(ct);

        _logger.LogInformation(
            "User {UserId} moved from company {OldCompanyId} to {NewCompanyId}",
            userId,
            oldCompanyId,
            companyId
        );
    }

    public async Task<(Stream Content, string ContentType)> GetLogoAsync(
        long companyId,
        CancellationToken ct = default
    )
    {
        var company = await _companyRepo.GetByIdAsync(companyId, ct)
            ?? throw new NotFoundException("Company", companyId);

        if (company.LogoStorageKey is null)
            throw new NotFoundException("Logo for company", companyId);

        var stored = await _storage.OpenReadAsync(company.LogoStorageKey, ct);

        return (stored.Content, ContentTypeFromKey(company.LogoStorageKey));
    }

    public async Task UploadLogoAsync(
        long companyId,
        Stream content,
        string contentType,
        CancellationToken ct = default
    )
    {
        var company = await _companyRepo.GetByIdAsync(companyId, ct)
            ?? throw new NotFoundException("Company", companyId);

        if (!content.CanSeek)
            throw new ArgumentException("Stream must be seekable.", nameof(content));

        EnsureLogoAccess(companyId);

        if (!LogoConstants.AllowedContentTypes.Contains(contentType))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["file"] = [$"Content type '{contentType}' is not allowed. Use image/png, image/jpeg, or image/webp."]
            });

        if (content.CanSeek && content.Length > LogoConstants.MaxSizeBytes)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["file"] = [$"Logo must not exceed {LogoConstants.MaxSizeBytes / (1024 * 1024)} MB."]
            });

        var header = new byte[12];
        var bytesRead = await content.ReadAsync(header.AsMemory(0, 12), ct);
        var detectedMime = _mimeDetector.Detect(header, bytesRead);

        if (detectedMime is null || !string.Equals(detectedMime, contentType, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["file"] = ["File content does not match the declared content type"]
            });

        var ext = GetExtension(contentType);
        var storageKey = $"logos/{companyId}/{Guid.NewGuid():N}.{ext}";
        var oldKey = company.LogoStorageKey;

        content.Position = 0;
        await _storage.SaveAsync(content, storageKey, contentType, ct);

        company.LogoStorageKey = storageKey;
        await _companyRepo.UpdateAsync(company, ct);
        await _uow.CommitAsync(ct);

        if (oldKey is not null)
            await _storage.DeleteAsync(oldKey, ct);

        _logger.LogInformation("Logo uploaded for company {CompanyId}: {StorageKey}", companyId, storageKey);
    }

    public async Task RemoveLogoAsync(long companyId, CancellationToken ct = default)
    {
        var company = await _companyRepo.GetByIdAsync(companyId, ct)
            ?? throw new NotFoundException("Company", companyId);

        EnsureLogoAccess(companyId);

        var oldKey = company.LogoStorageKey;
        company.LogoStorageKey = null;
        await _companyRepo.UpdateAsync(company, ct);
        await _uow.CommitAsync(ct);

        if (oldKey is not null)
            await _storage.DeleteAsync(oldKey, ct);

        _logger.LogInformation("Logo removed for company {CompanyId}", companyId);
    }

    private void EnsureLogoAccess(long companyId)
    {
        if (!_tenantContext.IsSet)
            throw new ForbiddenException(ErrorMessages.Company.TenantContextNotSet);

        if (_tenantContext.CompanyId != companyId)
            throw new ForbiddenException(ErrorMessages.Company.AccessDeniedLogo);
    }

    private static string GetExtension(string contentType) => contentType switch
    {
        "image/png" => "png",
        "image/jpeg" => "jpg",
        "image/webp" => "webp",
        _ => throw new InvalidOperationException($"Unsupported content type: {contentType}")
    };

    private static string ContentTypeFromKey(string storageKey) => Path.GetExtension(storageKey).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    private static CompanyResponse ToResponse(Company c) => new(
        c.Id,
        c.Code,
        c.Name,
        c.Address,
        c.Phone,
        c.Email,
        c.IsActive,
        c.CreatedAt,
        c.Users?.Count ?? 0,
        c.Warehouses?.Count ?? 0,
        c.LogoStorageKey is not null
    );
}
