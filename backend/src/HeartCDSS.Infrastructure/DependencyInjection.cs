using HeartCDSS.Application.Common.Interfaces;
using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Repositories;
using HeartCDSS.Infrastructure.Clients;
using HeartCDSS.Infrastructure.Persistence;
using HeartCDSS.Infrastructure.Persistence.Repositories;
using HeartCDSS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HeartCDSS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase");
        if (useInMemory)
        {
            var inMemoryDbName = configuration["InMemoryDbName"] ?? "pulse_cdss_test";
            services.AddDbContext<HeartCDSSDbContext>(options =>
            {
                options.UseInMemoryDatabase(inMemoryDbName);
            });
        }
        else
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=pulse_cdss;Username=postgres;Password=pulse_secure_password_dev";

            services.AddDbContext<HeartCDSSDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(HeartCDSSDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null
                    );
                });
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });
        }

        // Unit of Work registration
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<HeartCDSSDbContext>());

        // Repositories registration
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IClinicalEncounterRepository, ClinicalEncounterRepository>();
        services.AddScoped<IRiskAssessmentRepository, RiskAssessmentRepository>();
        services.AddScoped<IPredictionAuditRepository, PredictionAuditRepository>();

        // JWT Token Service
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Correlation Context Provider
        services.AddScoped<ICorrelationContext, HttpCorrelationContext>();

        // Resilient ML Inference HTTP Client with Polly pipeline
        var mlSection = configuration.GetSection("MlInference");
        var baseUrl = mlSection["BaseUrl"] ?? "http://localhost:8000";
        var timeoutMs = int.TryParse(mlSection["TimeoutMs"], out var t) ? t : 5000;
        var retryCount = int.TryParse(mlSection["RetryCount"], out var r) ? r : 3;

        services.AddHttpClient<IMlInferenceClient, FastApiInferenceClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan;
        })
        .AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
            options.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

            options.Retry.MaxRetryAttempts = retryCount;
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.Delay = TimeSpan.FromMilliseconds(200);
            options.Retry.UseJitter = true;

            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
