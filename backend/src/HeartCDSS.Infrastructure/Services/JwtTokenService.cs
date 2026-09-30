using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HeartCDSS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HeartCDSS.Infrastructure.Services;

/// <summary>
/// Infrastructure service issuing cryptographically signed JWT access tokens for clinical workflows.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string MakeToken(
        Guid userId,
        string username,
        string fullName,
        IEnumerable<string> roles,
        Guid? clinicianId = null)
    {
        var madeSecretKey = _configuration["Jwt:SecretKey"]
            ?? "PULSE_Clinical_Super_Secret_Jwt_Key_2026_CDSS_Enterprise_Token_Key_Security!";
        var madeIssuer = _configuration["Jwt:Issuer"] ?? "HeartCDSS.Api";
        var madeAudience = _configuration["Jwt:Audience"] ?? "HeartCDSS.Client";
        var madeExpiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60;

        var madeKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(madeSecretKey));
        var madeCredentials = new SigningCredentials(madeKey, SecurityAlgorithms.HmacSha256);

        var madeClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new("name", fullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("clinician_id", (clinicianId ?? userId).ToString())
        };

        foreach (var role in roles)
        {
            madeClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var madeTokenDescriptor = new JwtSecurityToken(
            issuer: madeIssuer,
            audience: madeAudience,
            claims: madeClaims,
            expires: DateTime.UtcNow.AddMinutes(madeExpiryMinutes),
            signingCredentials: madeCredentials);

        return new JwtSecurityTokenHandler().WriteToken(madeTokenDescriptor);
    }
}
