namespace WAMS.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Uoms;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.WorkflowTemplates;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.Common;

public partial class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly IUnitOfWork _uow;

    public DatabaseSeeder(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger,
        IUnitOfWork uow)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
        _uow = uow;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting database seeding...");

        // Seed company first - admin user needs to belong to a company
        var company = await SeedDefaultCompanyAsync();

        await SeedPermissionsAsync();
        await SeedRolesAsync();
        await SeedProvincesAsync();
        await AssignRolePermissionsAsync();
        await SeedInitialAdminAsync(company.Id);
        await SeedUomDefaultsAsync();
        await SeedActivityTypesAsync();
        await SeedWorkflowTemplatesAsync(company.Id);

        _logger.LogInformation("Database seeding completed");
    }

    private async Task<Company> SeedDefaultCompanyAsync()
    {
        // Use IgnoreQueryFilters - no tenant context during seeding
        var existing = await _context.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Code == (_configuration["InitialCompany:Code"] ?? "DEFAULT"));

        if (existing != null)
        {
            _logger.LogInformation("Default company already exists, skipping");
            return existing;
        }

        _logger.LogInformation("Creating default company...");

        var company = new Company
        {
            Code = _configuration["InitialCompany:Code"] ?? "DEFAULT",
            Name = _configuration["InitialCompany:Name"] ?? "Default Company",
            IsActive = true
        };

        _context.Companies.Add(company);
        await _uow.CommitAsync();

        _logger.LogInformation("Default company created: {Code} - {Name}", company.Code, company.Name);
        return company;
    }

    private async Task SeedPermissionsAsync()
    {
        _logger.LogInformation("Seeding permissions...");

        var existing = await _context.Permissions.ToDictionaryAsync(
            p => $"{p.Module}.{p.Resource}.{p.Action}");

        var permissions = PermissionSeeder.All;

        var toAdd = permissions.Where(p => !existing.ContainsKey($"{p.Module}.{p.Resource}.{p.Action}")).ToList();
        if (toAdd.Count > 0)
            _context.Permissions.AddRange(toAdd);

        // Descriptions are client-facing labels, synced here since they change often. Deletion is
        // not mirrored removing a permission row on a bad deploy could wipe client data, so
        // removals go through an explicit migration instead.
        var reworded = 0;
        foreach (var defined in permissions)
        {
            if (existing.TryGetValue($"{defined.Module}.{defined.Resource}.{defined.Action}", out var row)
                && row.Description != defined.Description)
            {
                row.Description = defined.Description;
                reworded++;
            }
        }

        if (toAdd.Count > 0 || reworded > 0)
            await _uow.CommitAsync();

        _logger.LogInformation(
            "Seeded {Count} new permissions, reworded {Reworded} descriptions ({Total} total defined)",
            toAdd.Count, reworded, permissions.Count);
    }

    private async Task SeedRolesAsync()
    {
        _logger.LogInformation("Seeding roles...");

        var defined = new List<Role>
        {
            new() { Name = RoleCodes.SuperAdmin,    DisplayName = "Super Administrator",    Description = "Full system access with all permissions",                                              IsSystem = true,  GlobalAccess = true  },

            new() { Name = RoleCodes.ItOps,     DisplayName = "IT Operations",         Description = "IT operations / system administration",            IsSystem = false, GlobalAccess = true  },
            new() { Name = RoleCodes.LogMgr,    DisplayName = "Logistics Manager",     Description = "Logistics management, head office",                IsSystem = false, GlobalAccess = true  },
            new() { Name = RoleCodes.LogSpv,    DisplayName = "Logistics Supervisor",  Description = "Logistics supervision, head office",               IsSystem = false, GlobalAccess = true  },
            new() { Name = RoleCodes.LogMktOps, DisplayName = "Logistics Market Ops",  Description = "Logistics market operations, head office",         IsSystem = false, GlobalAccess = true  },
            new() { Name = RoleCodes.WhMgr,     DisplayName = "Warehouse Manager",     Description = "Warehouse manager/coordinator, province-scoped",   IsSystem = false, GlobalAccess = false },
            new() { Name = RoleCodes.WhSpv,     DisplayName = "Warehouse Supervisor",  Description = "Warehouse supervisor under WH_MGR, province-scoped", IsSystem = false, GlobalAccess = false },
            new() { Name = RoleCodes.WhOps,     DisplayName = "Warehouse Operations",  Description = "Warehouse operations admin, province-scoped",      IsSystem = false, GlobalAccess = false },
            new() { Name = RoleCodes.FatMgr,    DisplayName = "Finance/Acct/Tax Mgr",  Description = "Finance, accounting & tax manager",                IsSystem = false, GlobalAccess = true  },
            new() { Name = RoleCodes.FatOps,    DisplayName = "Finance/Acct/Tax Ops",  Description = "Finance, accounting & tax operations",             IsSystem = false, GlobalAccess = true  },
        };

        var existingNames = await _context.Roles.IgnoreQueryFilters()
            .Select(r => r.Name)
            .ToHashSetAsync();

        var toAdd = defined.Where(r => !existingNames.Contains(r.Name)).ToList();

        if (toAdd.Count > 0)
        {
            _context.Roles.AddRange(toAdd);
            await _uow.CommitAsync();
        }

        _logger.LogInformation("Seeded {Count} new roles ({Total} total defined)", toAdd.Count, defined.Count);
    }

    private async Task SeedProvincesAsync()
    {
        _logger.LogInformation("Seeding provinces...");

        var existing = await _context.Provinces.IgnoreQueryFilters()
            .Select(p => p.Code).ToHashSetAsync();

        foreach (var (code, name, display, aliases) in ProvinceCodes.Seed)
        {
            if (existing.Contains(code)) continue;

            var province = new Province { Code = code, Name = name, Display = display, IsActive = true };
            foreach (var alias in aliases)
                province.Aliases.Add(new ProvinceAlias { Alias = ProvinceNormalizer.Normalize(alias) });

            _context.Provinces.Add(province);
        }

        await _uow.CommitAsync();
    }

    private async Task SeedActivityTypesAsync()
    {
        _logger.LogInformation("Seeding ActivityType defaults...");

        var defaults = new[]
        {
            (ActivityTypeCodes.Bongkar,   "Kegiatan Bongkar"),
            (ActivityTypeCodes.Muat,      "Kegiatan Muat"),
            (ActivityTypeCodes.Fumigasi,  "Fumigasi"),
            (ActivityTypeCodes.Opname,    "Opname"),
            (ActivityTypeCodes.Qc,        "Quality Control"),
            (ActivityTypeCodes.Gudang,    "Kegiatan Gudang"),
            (ActivityTypeCodes.AlatBerat, "Alat Berat"),
            (ActivityTypeCodes.Unbagging, "Unbagging"),
            (ActivityTypeCodes.Rebagging, "Rebagging"),
            (ActivityTypeCodes.Others,    "Others"),
        };

        var anyAdded = false;
        foreach (var (code, name) in defaults)
        {
            var existing = await _context.ActivityTypes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Code == code);

            if (existing is null)
            {
                _context.ActivityTypes.Add(new ActivityType { Code = code, Name = name });
                _logger.LogInformation("Seeding ActivityType: {Code}", code);
                anyAdded = true;
            }
        }

        if (anyAdded)
            await _uow.CommitAsync();
    }


    private async Task SeedInitialAdminAsync(long companyId)
    {
        var adminEmail = _configuration["InitialAdmin:Email"];
        var adminPassword = _configuration["InitialAdmin:Password"];
        var adminFullname = _configuration["InitialAdmin:Fullname"];

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            _logger.LogInformation("No initial admin credentials configured, skipping admin creation");
            return;
        }

        // Use IgnoreQueryFilters - no tenant context during seeding
        if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == adminEmail.ToLowerInvariant()))
        {
            _logger.LogInformation("Initial admin user already exists, skipping");
            return;
        }

        _logger.LogInformation("Creating initial admin user: {Email}", adminEmail);

        // Use IgnoreQueryFilters - no tenant context during seeding
        var superAdminRole = await _context.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == RoleCodes.SuperAdmin);
        if (superAdminRole == null)
        {
            _logger.LogWarning("SUPER_ADMIN role not found, cannot create initial admin");
            return;
        }

        var adminUser = new User
        {
            Email = adminEmail.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(adminPassword),
            Fullname = adminFullname ?? "System Administrator",
            CompanyId = companyId,
            IsActive = true
        };

        _context.Users.Add(adminUser);
        await _uow.CommitAsync();

        _context.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = superAdminRole.Id
        });

        await _uow.CommitAsync();
        _logger.LogInformation("Initial admin user created with SUPER_ADMIN role");
    }

    private async Task SeedUomDefaultsAsync()
    {
        if (await _context.UomMasters.IgnoreQueryFilters().AnyAsync())
        {
            _logger.LogInformation("UoM defaults already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding UoM defaults...");

        var uoms = new List<UomMaster>
        {
            new() { Code = "KG",      Name = "Kg",           IsActive = true },
            new() { Code = "HOUR",    Name = "Hour",         IsActive = true },
            new() { Code = "CNT20",   Name = "Container 20", IsActive = true },
            new() { Code = "CNT40",   Name = "Container 40", IsActive = true },
            new() { Code = "KMS",     Name = "Kemasan",      IsActive = true },
            new() { Code = "DAY",     Name = "Day",          IsActive = true },
            new() { Code = "MONTH",   Name = "Month",        IsActive = true },
            new() { Code = "BAG",     Name = "Bag",          IsActive = true },
            new() { Code = "PICKUP",  Name = "Pick Up",      IsActive = true },
            new() { Code = "CDIESEL", Name = "Colt Diesel",  IsActive = true },
            new() { Code = "FUSO",    Name = "Fuso",         IsActive = true },
            new() { Code = "METER",   Name = "Meter",        IsActive = true },
            new() { Code = "PERSON",  Name = "Per Orang",    IsActive = true },
        };

        _context.UomMasters.AddRange(uoms);

        await _uow.CommitAsync();

        _logger.LogInformation("Seeded {Count} UoM defaults", uoms.Count);
    }

    private async Task SeedWorkflowTemplatesAsync(long companyId)
    {
        _logger.LogInformation("Seeding workflow templates...");

        const string docType = WorkflowDocTypes.BudgetPlanApproval;

        var exists = await _context.WorkflowTemplates
            .IgnoreQueryFilters()
            .AnyAsync(t => t.CompanyId == companyId && t.DocType == docType);

        if (exists)
        {
            _logger.LogInformation("Workflow template for '{DocType}' already exists, skipping", docType);
            return;
        }

        var template = new WorkflowTemplate
        {
            CompanyId = companyId,
            DocType = docType,
            Name = "Budget Plan Approval",
            IsActive = true,
            Stages =
            [
                new WorkflowStage
                {
                    StageOrder = 1,
                    StageName = "Appr. Stage 1",
                    ApproverRoles = [RoleCodes.WhSpv],
                },
                new WorkflowStage
                {
                    StageOrder = 2,
                    StageName = "Appr. Stage 2",
                    ApproverRoles = [RoleCodes.WhMgr],
                },
            ],
        };

        _context.WorkflowTemplates.Add(template);
        await _uow.CommitAsync();

        _logger.LogInformation("Seeded workflow template '{Name}' with {Count} stages", template.Name, template.Stages.Count);
    }
}
