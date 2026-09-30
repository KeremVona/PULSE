using FluentAssertions;
using HeartCDSS.Api.Controllers;
using HeartCDSS.Application.Auth.Dtos;
using HeartCDSS.Application.Common.Interfaces;
using HeartCDSS.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HeartCDSS.Domain.Tests.Api;

public class AuthControllerTests
{
    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string MakeToken(Guid userId, string username, string fullName, IEnumerable<string> roles, Guid? clinicianId = null)
        {
            return "fake-jwt-token-string-xyz";
        }
    }

    private static AuthController MakeSut()
    {
        var madeTokenService = new FakeJwtTokenService();
        var madeController = new AuthController(madeTokenService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return madeController;
    }

    [Fact]
    public void MakeToken_WhenValidRequest_ReturnsOkWithAuthTokenResponseDto()
    {
        // Arrange
        var madeSut = MakeSut();
        var madeUserId = Guid.NewGuid();
        var madeRequest = new TokenRequestDto(
            Username: "dr.who",
            FullName: "The Doctor",
            UserId: madeUserId,
            Roles: [Role.Physician]
        );

        // Act
        var madeActionResult = madeSut.MakeToken(madeRequest);

        // Assert
        madeActionResult.Should().BeOfType<OkObjectResult>();
        var madeOkResult = madeActionResult as OkObjectResult;
        var response = madeOkResult!.Value as AuthTokenResponseDto;

        response.Should().NotBeNull();
        response!.AccessToken.Should().Be("fake-jwt-token-string-xyz");
        response.TokenType.Should().Be("Bearer");
        response.UserId.Should().Be(madeUserId);
        response.Roles.Should().Contain(Role.Physician);
    }

    [Fact]
    public void MakeToken_WhenUsernameEmpty_ReturnsBadRequestProblemDetails()
    {
        // Arrange
        var madeSut = MakeSut();
        var madeRequest = new TokenRequestDto(Username: "   ");

        // Act
        var madeActionResult = madeSut.MakeToken(madeRequest);

        // Assert
        madeActionResult.Should().BeOfType<BadRequestObjectResult>();
        var madeBadRequest = madeActionResult as BadRequestObjectResult;
        var problemDetails = madeBadRequest!.Value as ProblemDetails;

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Title.Should().Be("Invalid Credentials");
        problemDetails.Detail.Should().Contain("cannot be empty");
    }
}
