namespace HeartCDSS.Application.Common.Interfaces;

/// <summary>
/// Provides access to ambient distributed tracing correlation ID and client metadata.
/// </summary>
public interface ICorrelationContext
{
    Guid CorrelationId { get; }
    string? ClientIp { get; }
    string? UserAgent { get; }
}
