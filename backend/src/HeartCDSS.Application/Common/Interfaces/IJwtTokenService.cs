namespace HeartCDSS.Application.Common.Interfaces;

/// <summary>
/// Service contract for generating signed JSON Web Tokens for clinical authentication.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Makes a signed JWT Bearer access token containing user identity and clinical roles.
    /// </summary>
    string MakeToken(
        Guid userId,
        string username,
        string fullName,
        IEnumerable<string> roles,
        Guid? clinicianId = null);
}
