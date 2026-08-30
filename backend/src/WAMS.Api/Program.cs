using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using WAMS.Api.Middleware;
using WAMS.Application;
using WAMS.Application.Common;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.Common;
using WAMS.Infrastructure;
using WAMS.Infrastructure.Caching.Common;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Observability;

// Serilog Bootstrap
var minLevel = Enum.TryParse<LogEventLevel>(
    Environment.GetEnvironmentVariable("Logging__MinLevel"), ignoreCase: true, out var parsedLevel)
    ? parsedLevel
    : LogEventLevel.Information;

var writeToFile = !string.Equals(
    Environment.GetEnvironmentVariable("Logging__WriteToFile"), "false", StringComparison.OrdinalIgnoreCase);

const string logPath = "logs/wams-.log";
const int logRetainDays = 30;

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Is(minLevel)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

if (writeToFile) loggerConfig.WriteTo.Async(a => a.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: logRetainDays));

Log.Logger = loggerConfig.CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    // QuestPDF license is required to use the library. The community edition is free to use and does not require registration.
    // set here explicitly to avoid the warning message.
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    builder.Host.UseSerilog();

    var port = builder.Configuration["PORT"] ?? "8080";
    builder.WebHost.UseUrls($"http://*:{port}");

    Log.Information("Starting WAMS API");
    Log.Information("Description: Warehouse Management System API");
    Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
    Log.Information("Port: {Port}", port);
    Log.Information("Health Check: /health");

    // Database
    var dbConnStr = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddMemoryCache();

    // Item entities (BudgetPlanItem, RateCardItem, etc.) are always accessed via their parent navigation
    // or with explicit parent-scoped WHERE clauses - the filter interaction warning is a false alarm here.
    static void ConfigureDb(DbContextOptionsBuilder options, string? connStr) => options
        .UseNpgsql(connStr, npgsql => npgsql.EnableRetryOnFailure(3))
        .ConfigureWarnings(w => w.Ignore(
            CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));

    builder.Services.AddDbContext<AppDbContext>(o => ConfigureDb(o, dbConnStr));

    // Factory is required by background services (singleton lifetime cannot use scoped DbContext directly)
    builder.Services.AddDbContextFactory<AppDbContext>(o => ConfigureDb(o, dbConnStr),
        ServiceLifetime.Scoped);

    // HybridCache: local in-process memory only.
    // Per-entry TTLs are configured via WamsCacheOptions; these global defaults are the safety net.
    builder.Services.AddHybridCache(opts =>
    {
        opts.MaximumPayloadBytes = 1024 * 1024; // 1 MB hard limit per serialised entry
        opts.MaximumKeyLength = 512;
        opts.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
        {
            LocalCacheExpiration = TimeSpan.FromMinutes(5),
            Expiration = TimeSpan.FromMinutes(5),
        };
    });

    // Cache TTL configuration - all values overridable via appsettings / env vars
    builder.Services.Configure<WamsCacheOptions>(builder.Configuration.GetSection("Cache"));

    // Authentication
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("JWT secret is not configured");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // disable claim type mapping to preserve original JWT claim names (e.g., "sub" instead of ClaimTypes.NameIdentifier)
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "WAMS",
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };

            // <img>/<a> tags can't send an Authorization header, so allow the
            // access token via query string, but only for the file-serving route - every
            // other endpoint still requires the header.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (string.IsNullOrEmpty(context.Token) &&
                        context.Request.Path.StartsWithSegments("/api/v1/files") &&
                        context.Request.Query.TryGetValue("token", out var token))
                    {
                        context.Token = token;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddHttpContextAccessor();
    builder.Services.Configure<FileAttachmentOptions>(builder.Configuration.GetSection(FileAttachmentOptions.SectionName));
    builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection(NotificationOptions.SectionName));
    builder.Services.Configure<BudgetPlanReminderOptions>(builder.Configuration.GetSection(BudgetPlanReminderOptions.SectionName));
    builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
    builder.Services.Configure<WAMS.Application.Export.ExportOptions>(builder.Configuration.GetSection(WAMS.Application.Export.ExportOptions.SectionName));
    builder.Services.Configure<PdfOptions>(builder.Configuration.GetSection(PdfOptions.SectionName));

    var fileOpts = builder.Configuration
        .GetSection(FileAttachmentOptions.SectionName)
        .Get<FileAttachmentOptions>() ?? new FileAttachmentOptions();
    var maxBodySize = fileOpts.MaxFileSizeBytes + 1024;

    builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = maxBodySize);
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
    {
        o.MultipartBodyLengthLimit = fileOpts.MaxFileSizeBytes;
        o.ValueLengthLimit = int.MaxValue;
    });

    // Repositories, business services, cache decorators, external sync, SAP client, email, seeders.
    builder.Services
        .AddApplicationServices()
        .AddInfrastructureServices(builder.Configuration);

    // Response compression (Brotli preferred, Gzip fallback; skips small responses and pre-compressed content automatically)
    builder.Services.AddResponseCompression(opts =>
    {
        opts.EnableForHttps = true;
        opts.Providers.Add<BrotliCompressionProvider>();
        opts.Providers.Add<GzipCompressionProvider>();
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(opts => opts.Level = System.IO.Compression.CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(opts => opts.Level = System.IO.Compression.CompressionLevel.Fastest);

    // Rate limiting - sliding window per IP on auth endpoints
    var authRateLimit = builder.Configuration.GetValue("RateLimit:Auth:PermitLimit", 10);
    var authRateWindowSeconds = builder.Configuration.GetValue("RateLimit:Auth:WindowSeconds", 60);

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = authRateLimit,
                    Window = TimeSpan.FromSeconds(authRateWindowSeconds),
                    SegmentsPerWindow = 4,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));
    });

    // Health checks - PostgreSQL
    var healthDbConnStr = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured");

    builder.Services.AddHealthChecks()
        .AddNpgSql(healthDbConnStr, name: "postgres", failureStatus: HealthStatus.Unhealthy, tags: ["db"]);

    // OpenTelemetry - distributed tracing + metrics
    var otelEnabled = builder.Configuration.GetValue("OpenTelemetry:Enabled", false);
    var otelServiceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "wams";
    var otelEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
    var prometheusEnabled = builder.Configuration.GetValue("OpenTelemetry:Prometheus:Enabled", false);
    var tracingSamplingRatio = builder.Configuration.GetValue("OpenTelemetry:Tracing:SamplingRatio", 1.0);

    // WamsMetrics is always registered - it's lightweight (in-process counters backed by IMeterFactory).
    // When OTel is disabled, metrics are still recorded but simply not exported anywhere.
    builder.Services.AddSingleton<IWamsMetrics, WamsMetrics>();

    if (otelEnabled)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(otelServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new TraceIdRatioBasedSampler(tracingSamplingRatio))
                    .AddAspNetCoreInstrumentation(opts => opts.RecordException = true)
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = new Uri(otelEndpoint));
            })
            .WithMetrics(meterBuilder =>
            {
                meterBuilder
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(WamsMetrics.MeterName)
                    .AddOtlpExporter(o => o.Endpoint = new Uri(otelEndpoint));

                if (prometheusEnabled)
                    meterBuilder.AddPrometheusExporter();
            });

        Log.Information("OpenTelemetry enabled. Service={Service} Endpoint={Endpoint}", otelServiceName, otelEndpoint);
        if (prometheusEnabled)
            Log.Information("Prometheus scrape endpoint enabled at /metrics");
    }
    else
    {
        Log.Information("OpenTelemetry is DISABLED (OpenTelemetry:Enabled=false)");
    }

    // CORS
    var corsOrigins = builder.Configuration["CORS:Origins"] ?? "http://localhost:5173";
    var allowCredentials = builder.Configuration["CORS:AllowCredentials"] != "false";
    var origins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultCorsPolicy", policy =>
        {
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();

            if (allowCredentials)
            {
                policy.AllowCredentials();
            }
        });
    });

    // Controllers & Swagger
    builder.Services.AddControllers()
        .AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.Converters.Add(new Ardalis.SmartEnum.SystemTextJson.SmartEnumNameConverter<WAMS.Domain.Enums.BudgetPlanStatus, string>());
            opt.JsonSerializerOptions.Converters.Add(new Ardalis.SmartEnum.SystemTextJson.SmartEnumNameConverter<WAMS.Domain.Enums.BudgetTemplateStatus, string>());
            opt.JsonSerializerOptions.Converters.Add(new Ardalis.SmartEnum.SystemTextJson.SmartEnumNameConverter<WAMS.Domain.Enums.BudgetPlanType, string>());
            opt.JsonSerializerOptions.Converters.Add(new Ardalis.SmartEnum.SystemTextJson.SmartEnumNameConverter<WAMS.Domain.Enums.RateCardStatus, string>());
            opt.JsonSerializerOptions.Converters.Add(new Ardalis.SmartEnum.SystemTextJson.SmartEnumNameConverter<WAMS.Domain.Enums.PurchaseOrderStatus, string>());
            opt.JsonSerializerOptions.Converters.Add(new Ardalis.SmartEnum.SystemTextJson.SmartEnumNameConverter<WAMS.Domain.Enums.WorkOrderStatus, string>());
            opt.JsonSerializerOptions.Converters.Add(new Ardalis.SmartEnum.SystemTextJson.SmartEnumNameConverter<WAMS.Domain.Enums.RecapWorkOrderStatus, string>());
            opt.JsonSerializerOptions.Converters.Add(new Ardalis.SmartEnum.SystemTextJson.SmartEnumNameConverter<WAMS.Domain.Enums.AccountPayableStatus, string>());
            opt.JsonSerializerOptions.Converters.Add(new Ardalis.SmartEnum.SystemTextJson.SmartEnumNameConverter<WAMS.Domain.Enums.TaxCategory, string>());
            opt.JsonSerializerOptions.Converters.Add(new WAMS.Api.Json.UtcDateTimeConverter());
            opt.JsonSerializerOptions.Converters.Add(new WAMS.Api.Json.UtcNullableDateTimeConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "WAMS API",
            Version = "v1",
            Description = "Warehouse Management System API"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement((OpenApiDocument doc) => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer", doc),
                new List<string>()
            }
        });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (System.IO.File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // Middleware Pipeline (ORDER MATTERS)
    // Response compression (outermost so it wraps all responses including health)
    app.UseResponseCompression();

    // SpreadCheetah uses ZipArchive internally. ZipArchiveEntry.WriteDataDescriptor() writes
    // the ZIP entry trailer synchronously. Enable sync I/O on export paths to allow it.
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.Value?.EndsWith("/export", StringComparison.OrdinalIgnoreCase) == true)
        {
            var bodyControl = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature>();
            if (bodyControl is not null) bodyControl.AllowSynchronousIO = true;
        }

        await next(context);
    });

    // Request ID (sets ID before anything else logs)
    app.UseMiddleware<RequestIdMiddleware>();

    // Request logging (wraps exception handler so it sees the final status code, not raw exceptions)
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var requestId = httpContext.Items["RequestId"]?.ToString();
            if (!string.IsNullOrEmpty(requestId))
            {
                diagnosticContext.Set("RequestId", requestId);
            }
        };
    });

    // Exception handling (inside Serilog so errors are mapped to proper status codes before Serilog logs)
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // CORS - must be before auth to allow preflight requests
    app.UseCors("DefaultCorsPolicy");

    // Rate limiting
    app.UseRateLimiter();

    // Swagger (dev only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Auth
    app.UseAuthentication();
    app.UseMiddleware<TenantMiddleware>();
    app.UseMiddleware<WarehouseMiddleware>();
    app.UseAuthorization();

    // Controllers
    app.MapControllers();

    // Prometheus metrics scrape (only when enabled - don't expose in prod without auth proxy)
    if (prometheusEnabled)
        app.MapPrometheusScrapingEndpoint();

    // Health check - PostgreSQL liveness
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (ctx, report) =>
        {
            ctx.Response.ContentType = "application/json";
            var result = new
            {
                status = report.Status.ToString().ToLowerInvariant(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString().ToLowerInvariant(),
                    duration = e.Value.Duration.TotalMilliseconds
                })
            };
            await ctx.Response.WriteAsJsonAsync(result);
        }
    });

    // Database Migration & Seeding
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Auto-migrate on startup 
        var autoMigrateEnabled = app.Configuration.GetValue("Database:AutoMigrate", true);
        if (autoMigrateEnabled)
        {
            await db.Database.MigrateAsync();
            Log.Information("Database migrations applied");
        }
        else
        {
            Log.Information("Database:AutoMigrate is false; skipping automatic migration on startup");
        }

        var autoSeedEnabled = app.Configuration.GetValue("Database:AutoSeed", true);
        if (autoSeedEnabled)
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
        else
        {
            Log.Information("Database:AutoSeed is false; skipping automatic seeding on startup");
        }
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
