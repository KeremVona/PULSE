namespace HeartCDSS.Application.Auth.Dtos;

/// <summary>
/// Request payload for obtaining a clinical JWT bearer access token.
/// </summary>
public sealed record TokenRequestDto(
    string Username,
    string? FullName = null,
    Guid? UserId = null,
    IReadOnlyList<string>? Roles = null
);

/// <summary>
/// Response payload delivering an authenticated JWT Bearer token and clinician identity metadata.
/// </summary>
public sealed record AuthTokenResponseDto(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds,
    Guid UserId,
    IReadOnlyList<string> Roles
);
