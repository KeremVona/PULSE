using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace HeartCDSS.Domain.Tests.Infrastructure;

public class JwtTokenServiceTests
{
    private static IConfiguration MakeConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "PULSE_Clinical_Super_Secret_Jwt_Key_2026_CDSS_Enterprise_Token_Key_Security!",
            ["Jwt:Issuer"] = "HeartCDSS.Api",
            ["Jwt:Audience"] = "HeartCDSS.Client",
            ["Jwt:ExpiryMinutes"] = "120"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static JwtTokenService MakeSut()
    {
        return new JwtTokenService(MakeConfiguration());
    }

    [Fact]
    public void MakeToken_ShouldGenerateValidSignedJwtWithExpectedClaims()
    {
        // Arrange
        var madeSut = MakeSut();
        var madeUserId = Guid.NewGuid();
        var madeClinicianId = Guid.NewGuid();
        var madeUsername = "dr.house";
        var madeFullName = "Dr. Gregory House";
        var madeRoles = new[] { Role.Physician, Role.ClinicalStaff };

        // Act
        var madeTokenString = madeSut.MakeToken(
            userId: madeUserId,
            username: madeUsername,
            fullName: madeFullName,
            roles: madeRoles,
            clinicianId: madeClinicianId
        );

        // Assert
        madeTokenString.Should().NotBeNullOrWhiteSpace();

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.CanReadToken(madeTokenString).Should().BeTrue();

        var parsedToken = tokenHandler.ReadJwtToken(madeTokenString);
        parsedToken.Issuer.Should().Be("HeartCDSS.Api");
        parsedToken.Audiences.Should().Contain("HeartCDSS.Client");

        var subClaim = parsedToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be(madeUserId.ToString());

        var nameClaim = parsedToken.Claims.FirstOrDefault(c => c.Type == "name");
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be(madeFullName);

        var clinicianClaim = parsedToken.Claims.FirstOrDefault(c => c.Type == "clinician_id");
        clinicianClaim.Should().NotBeNull();
        clinicianClaim!.Value.Should().Be(madeClinicianId.ToString());

        var roleClaims = parsedToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Should().Contain(Role.Physician);
        roleClaims.Should().Contain(Role.ClinicalStaff);

        // Validate cryptographically with signing key
        var tokenValidationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "HeartCDSS.Api",
            ValidAudience = "HeartCDSS.Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("PULSE_Clinical_Super_Secret_Jwt_Key_2026_CDSS_Enterprise_Token_Key_Security!"))
        };

        var principal = tokenHandler.ValidateToken(madeTokenString, tokenValidationParams, out var validatedToken);
        principal.Should().NotBeNull();
        validatedToken.Should().NotBeNull();
    }
}
