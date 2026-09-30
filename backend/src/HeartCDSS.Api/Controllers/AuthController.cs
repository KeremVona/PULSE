using HeartCDSS.Application.Auth.Dtos;
using HeartCDSS.Application.Common.Interfaces;
using HeartCDSS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeartCDSS.Api.Controllers;

/// <summary>
/// Controller managing clinical authentication, session establishment, and JWT bearer token issuance.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(IJwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    }

    /// <summary>
    /// Issues a signed JWT access token for clinical personnel.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult MakeToken([FromBody] TokenRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Invalid Credentials",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Username cannot be empty.",
                Instance = HttpContext.Request.Path
            });
        }

        var roles = request.Roles != null && request.Roles.Count > 0
            ? request.Roles
            : [Role.Physician];

        var userId = request.UserId ?? Guid.NewGuid();
        var madeToken = _jwtTokenService.MakeToken(
            userId: userId,
            username: request.Username,
            fullName: request.FullName ?? request.Username,
            roles: roles,
            clinicianId: userId
        );

        var madeResponse = new AuthTokenResponseDto(
            AccessToken: madeToken,
            TokenType: "Bearer",
            ExpiresInSeconds: 3600,
            UserId: userId,
            Roles: roles
        );

        return Ok(madeResponse);
    }
}
