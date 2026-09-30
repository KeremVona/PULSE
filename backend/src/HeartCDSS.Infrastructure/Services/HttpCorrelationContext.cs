using HeartCDSS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace HeartCDSS.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of ICorrelationContext extracting ambient tracing headers and HTTP connection metadata.
/// </summary>
public sealed class HttpCorrelationContext : ICorrelationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCorrelationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid CorrelationId
    {
        get
        {
            if (_httpContextAccessor?.HttpContext?.Request.Headers.TryGetValue("X-Correlation-Id", out var headVal) == true
                && Guid.TryParse(headVal, out var parsedCorr))
            {
                return parsedCorr;
            }

            return Guid.NewGuid();
        }
    }

    public string? ClientIp => _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _httpContextAccessor?.HttpContext?.Request.Headers.UserAgent.ToString();
}
