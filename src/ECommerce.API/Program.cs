using System.Threading.RateLimiting;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Jobs;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Seed;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ECommerce.Application;
using ECommerce.Application.Common.Interfaces;
using ECommerce.API.Jobs;
using ECommerce.API.Services;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.DependencyInjection;
using ECommerce.Persistence;
using Serilog;
using Serilog.Events;
using Asp.Versioning;
using Microsoft.Extensions.Options;

// ── Serilog bootstrap logger (uygulama başlamadan önce hataları yakala) ───────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("ECommerce API başlatılıyor...");

    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

    // ── Serilog tam konfigürasyonu ────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/ecommerce-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    );

    // ── Katman servisleri ─────────────────────────────────────────────────────
    builder.Services.AddApplicationServices();
    // Fraud entegrasyonu (varsayılan kapalı). Geçersiz Mode değeri açılışta hata verir.
    builder.Services.AddSingleton(builder.Configuration
        .GetSection(ECommerce.Application.Common.Fraud.FraudDetectionSettings.SectionName)
        .Get<ECommerce.Application.Common.Fraud.FraudDetectionSettings>() ?? new());
    builder.Services.AddPersistenceServices(builder.Configuration);
    builder.Services.AddCartServices(builder.Configuration);

    // ── PostgreSQL ────────────────────────────────────────────────────────────
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ── ASP.NET Core Identity ─────────────────────────────────────────────────
    builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        // Brute-force koruması: 5 hatalı denemede hesap 15 dk kilitlenir.
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // ── JWT + Infrastructure (Identity'den SONRA) ─────────────────────────────
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // ── Hangfire ──────────────────────────────────────────────────────────────
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c =>
            c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 2;
        options.Queues = ["default", "outbox"];
    });

    // OutboxProcessorJob'u DI'a kaydet
    builder.Services.AddScoped<OutboxProcessorJob>();
    builder.Services.AddScoped<IProductImageStorage, LocalProductImageStorage>();
    builder.Services.AddScoped<IProfileImageStorage, LocalProfileImageStorage>();
    builder.Services.AddScoped<ProductImageCleanupJob>();

    // ── Authorization Policies ────────────────────────────────────────────────
    builder.Services.AddMarketplaceAuthorization();
    builder.Services.AddHealthChecks();

    // ── Rate Limiting ─────────────────────────────────────────────────────────
    // İstemci başına bölümlenmiş limitler + yalnızca güvenilir proxy'lerden X-Forwarded-For.
    builder.Services.AddTrustedForwardedHeaders(builder.Configuration);
    builder.Services.AddECommerceRateLimiting();

    // ── API Versioning ────────────────────────────────────────────────────────
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version")
        );
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // ── Swagger ───────────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ECommerce API",
            Version = "v1",
            Description = "Clean Architecture + CQRS tabanlı E-Ticaret RESTful API",
            Contact = new OpenApiContact { Name = "ECommerce API" }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT token giriniz. Örnek: eyJhbGciOi..."
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // ── Serilog request logging ───────────────────────────────────────────────
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} yanıtlandı {StatusCode} ({Elapsed:0.0000} ms)";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : LogEventLevel.Information;
    });

    // ── Seed (migration + roller + admin kullanıcı) ───────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        // Bilinen parolalı demo hesaplar hiçbir koşulda Development dışında oluşturulmaz.
        var seedDemoUsers = builder.Configuration.GetValue<bool>("SeedDemoUsers:Enabled");
        if (seedDemoUsers && !app.Environment.IsDevelopment())
        {
            Log.Warning("SeedDemoUsers:Enabled yok sayıldı: demo hesaplar yalnızca Development ortamında oluşturulur.");
            seedDemoUsers = false;
        }
        await IdentitySeeder.SeedAsync(scope.ServiceProvider, seedDemoUsers);
    }

    // ── Hangfire recurring job kaydı ─────────────────────────────────────────
    var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<OutboxProcessorJob>(
        recurringJobId: "outbox-processor",
        methodCall: job => job.ProcessAsync(),
        cronExpression: "* * * * *",
        options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    recurringJobManager.AddOrUpdate<ProductImageCleanupJob>(
        recurringJobId: "product-image-cleanup",
        methodCall: job => job.ProcessAsync(CancellationToken.None),
        cronExpression: "15 3 * * *",
        options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    // ── Middleware pipeline ───────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // İlk sırada: sonraki tüm bileşenler (rate limit, refresh token IP kaydı) gerçek istemci IP'sini görür.
    app.UseForwardedHeaders();
    app.UseMiddleware<ECommerce.API.Middlewares.GlobalExceptionMiddleware>();
    app.UseMiddleware<ECommerce.API.Middlewares.SecurityHeadersMiddleware>();
    app.UseMiddleware<ECommerce.API.Middlewares.ETagMiddleware>();
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    // Kimlik doğrulamadan SONRA: "api" limiti oturum açmış kullanıcı başına bölümlenebilsin.
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    // ── Hangfire Dashboard (geliştirme ortamı) ────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = []
        });
    }

    app.MapControllers();
    app.MapHealthChecks("/health").DisableRateLimiting();

    Log.Information("ECommerce API hazır, istekler dinleniyor.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ECommerce API beklenmedik hata nedeniyle sonlandırıldı.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
