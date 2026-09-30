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

app.UseHttpsRedirection();

app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "PULSE ASP.NET Core Enterprise Orchestration Layer",
    Timestamp = DateTimeOffset.UtcNow
}))
.WithName("HealthCheck");

// Enable Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Partial program class for WebApplicationFactory integration testing
public partial class Program { }
