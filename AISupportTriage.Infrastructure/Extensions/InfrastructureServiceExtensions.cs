using AISupportTriage.Application.Interfaces;
using AISupportTriage.Infrastructure.AI;
using AISupportTriage.Infrastructure.Data;
using AISupportTriage.Infrastructure.Hubs;
using AISupportTriage.Infrastructure.Jobs;
using AISupportTriage.Infrastructure.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AISupportTriage.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("AISupportTriage.Infrastructure")));

        services.AddScoped<IApplicationDbContext>(
            provider => provider.GetRequiredService<ApplicationDbContext>());

        // Tenant & User Services
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();

        // AI Service
        services.AddHttpClient<IAiTriageService, OpenAiTriageService>();

        // Background Jobs (Hangfire)
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                configuration.GetConnectionString("DefaultConnection"),
                new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

        services.AddHangfireServer();

        // Register job classes for DI
        services.AddScoped<AiAnalysisJob>();
        services.AddScoped<SlaCheckJob>();

        // SignalR
        services.AddSignalR();

        return services;
    }
}