using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using UniversityAPI.Controllers;
using UniversityAPI.Models;

namespace UniversityAPI.Tests;

[TestFixture]
public class AuthControllerTests
{
    [Test]
    public void Login_AdminCredentials_ReturnsJwtWithAdminRole()
    {
        // Create controller and login request for admin user
        var controller = new AuthController();
        var request = new LoginRequestDto
        {
            Email = "admin@dkit.ie",
            Password = "Admin123!"
        };

        // Login action
        var result = controller.Login(request);

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult, "Expected OkObjectResult");

        var response = okResult!.Value as LoginResponseDto;
        Assert.IsNotNull(response, "Expected LoginResponseDto");

        // Basic checks on response fields
        Assert.That(response!.Role, Is.EqualTo("Admin"));
        Assert.That(response.StudentId, Is.Null);
        Assert.IsFalse(string.IsNullOrWhiteSpace(response.AccessToken), "AccessToken should not be empty");

        // JwtSecurityTokenHandler can parse and inspect JWT tokens
        var handler = new JwtSecurityTokenHandler();

        // CanReadToken checks if string looks like a valid JWT (header.payload.signature)
        Assert.That(handler.CanReadToken(response.AccessToken), Is.True);

        // ReadJwtToken parses token without validating signature, so we can inspect claims
        var token = handler.ReadJwtToken(response.AccessToken);

        // Issuer should match value used in AuthController
        Assert.That(token.Issuer, Is.EqualTo("UniversityAPI"));

        // Role claim is used by [Authorize(Roles)] in controllers
        var roleClaim = token.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.IsNotNull(roleClaim, "Role claim present");
        Assert.That(roleClaim!.Value, Is.EqualTo("Admin"));

        // Subject claim was set to user email
        Assert.That(token.Subject, Is.EqualTo("admin@dkit.ie"));
    }

    [Test]
    public void Login_StudentCredentials_ReturnsJwtWithStudentRoleAndStudentIdClaim()
    {
        var controller = new AuthController();
        var request = new LoginRequestDto
        {
            Email = "student00128@dkit.ie",
            Password = "Student123!"
        };

        var result = controller.Login(request);

        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult, "Expected OkObjectResult");

        var response = okResult!.Value as LoginResponseDto;
        Assert.IsNotNull(response, "Expected LoginResponseDto");

        // Student should have Student role and StudentId = 1
        Assert.That(response!.Role, Is.EqualTo("Student"));
        Assert.That(response.StudentId, Is.EqualTo(1));
        Assert.IsFalse(string.IsNullOrWhiteSpace(response.AccessToken));

        var handler = new JwtSecurityTokenHandler();
        Assert.That(handler.CanReadToken(response.AccessToken), Is.True);

        var token = handler.ReadJwtToken(response.AccessToken);

        // Role claim should be "Student"
        var roleClaim = token.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.IsNotNull(roleClaim, "Role claim present");
        Assert.That(roleClaim!.Value, Is.EqualTo("Student"));

        // Custom "studentId" claim links token to Student entity
        var studentIdClaim = token.Claims.SingleOrDefault(c => c.Type == "studentId");
        Assert.IsNotNull(studentIdClaim, "studentId claim present");
        Assert.That(studentIdClaim!.Value, Is.EqualTo("1"));
    }

    [Test]
    public void Login_InvalidPassword_ReturnsUnauthorized()
    {
        var controller = new AuthController();
        var request = new LoginRequestDto
        {
            Email = "admin@dkit.ie",
            Password = "Admin12345!"
        };

        var result = controller.Login(request);

        // Controller should return 401 Unauthorized for bad credentials
        Assert.IsInstanceOf<UnauthorizedResult>(result);
    }
}
