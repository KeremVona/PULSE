using HeartCDSS.Api.Middleware;
using HeartCDSS.Application;
using HeartCDSS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register Application Layer (MediatR, FluentValidation, Pipeline Behaviors)
builder.Services.AddApplication();

// Register Infrastructure Layer (EF Core PostgreSQL, Repositories, Unit of Work)
builder.Services.AddInfrastructure(builder.Configuration);

// Add HttpContextAccessor for distributed tracing & correlation ID propagation
builder.Services.AddHttpContextAccessor();

// Configure JWT Authentication & Authorization
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"] ?? "PULSE_Clinical_Super_Secret_Jwt_Key_2026_CDSS_Enterprise_Token_Key_Security!";
var issuer = jwtSection["Issuer"] ?? "HeartCDSS.Api";
var audience = jwtSection["Audience"] ?? "HeartCDSS.Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("PulseCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAuthorization();

// Add Controllers & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "PULSE - Clinical Decision Support System (CDSS) API",
            Version = "v1.0.0",
            Description = "Production-grade Clinical Decision Support System (CDSS) for Cardiovascular Risk Stratification & Explainable AI."
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Global Exception Handling Middleware (RFC 7807 ProblemDetails)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Apply pending database migrations on startup when running with relational database
using (var madeScope = app.Services.CreateScope())
{
    var madeServices = madeScope.ServiceProvider;
    var madeConfig = madeServices.GetRequiredService<IConfiguration>();
    if (!madeConfig.GetValue<bool>("UseInMemoryDatabase"))
    {
        var madeLogger = madeServices.GetRequiredService<ILogger<Program>>();
        var madeDbContext = madeServices.GetRequiredService<HeartCDSS.Infrastructure.Persistence.HeartCDSSDbContext>();
        var maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                madeLogger.LogInformation("Applying relational database migrations (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
                Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.Migrate(madeDbContext.Database);
                madeLogger.LogInformation("Database migration applied successfully.");

                if (!madeDbContext.Patients.Any())
                {
                    var seedPatient = HeartCDSS.Domain.Entities.Patient.Make(
                        mrn: "MRN-PULSE-001",
                        firstName: "Jane",
                        lastName: "Doe",
                        dateOfBirth: new DateOnly(1970, 5, 15),
                        biologicalSex: HeartCDSS.Domain.Enums.BiologicalSex.Female,
                        id: Guid.Parse("11111111-1111-1111-1111-111111111111")
                    );
                    madeDbContext.Patients.Add(seedPatient);
                    madeDbContext.SaveChanges();
                    madeLogger.LogInformation("Seeded default patient record {Mrn} ({PatientId}).", seedPatient.Mrn, seedPatient.Id);
                }

                if (!madeDbContext.MlModels.Any())
                {
                    var seedModel = HeartCDSS.Domain.Entities.MlModel.Make(
                        modelName: "PULSE-XGBoost-Calibrated",
                        modelVersion: "1.0.0",
                        modelFamily: "GradientBoosting",
                        calibrationMethod: "Isotonic",
                        decisionThreshold: 0.500m,
                        targetSensitivity: 0.900m,
                        rocAuc: 0.892m,
                        prAuc: 0.865m,
                        brierScore: 0.082m,
                        id: Guid.Parse("00000000-0000-0000-0000-000000000001")
                    );
                    madeDbContext.MlModels.Add(seedModel);
                    madeDbContext.SaveChanges();
                    madeLogger.LogInformation("Seeded default ML model record {ModelName} ({ModelId}).", seedModel.ModelName, seedModel.Id);
                }

                if (!madeDbContext.Roles.Any())
                {
                    madeDbContext.Roles.AddRange(
                        new HeartCDSS.Domain.Entities.Role(Guid.Parse("11111111-1111-1111-1111-111111111111"), HeartCDSS.Domain.Entities.Role.Physician, "Cardiologists and attending physicians with clinical order and assessment authority"),
                        new HeartCDSS.Domain.Entities.Role(Guid.Parse("22222222-2222-2222-2222-222222222222"), HeartCDSS.Domain.Entities.Role.ClinicalStaff, "Nurses and medical assistants authorized to intake vitals and view patient rosters"),
                        new HeartCDSS.Domain.Entities.Role(Guid.Parse("33333333-3333-3333-3333-333333333333"), HeartCDSS.Domain.Entities.Role.Admin, "System administrators managing user accounts and model registries"),
                        new HeartCDSS.Domain.Entities.Role(Guid.Parse("44444444-4444-4444-4444-444444444444"), HeartCDSS.Domain.Entities.Role.Auditor, "Compliance auditors with read-only access to prediction logs and audit trails")
                    );
                    madeDbContext.SaveChanges();
                    madeLogger.LogInformation("Seeded default role records.");
                }

                break;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                madeLogger.LogWarning(ex, "Database migration attempt {Attempt} failed. Retrying in {Delay}s...", attempt, retryDelay.TotalSeconds);
                Thread.Sleep(retryDelay);
            }
            catch (Exception ex)
            {
                madeLogger.LogError(ex, "Database migration failed after {MaxRetries} attempts.", maxRetries);
                throw;
            }
        }
    }
}

if (builder.Configuration.GetValue<bool>("EnableHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "PULSE ASP.NET Core Enterprise Orchestration Layer",
    Timestamp = DateTimeOffset.UtcNow
}))
.WithName("HealthCheckRoot");

app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "PULSE ASP.NET Core Enterprise Orchestration Layer",
    Timestamp = DateTimeOffset.UtcNow
}))
.WithName("HealthCheck");

// Enable CORS, Authentication and Authorization
app.UseCors("PulseCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Partial program class for WebApplicationFactory integration testing
public partial class Program { }
