using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using UniversityAPI.Models;

namespace UniversityAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // In-memory users store
    private static readonly List<UniversityUser> Users = new List<UniversityUser>
    {
        new UniversityUser
        {
            Email = "admin@dkit.ie",
            Password = "Admin123!",
            Role = "Admin",
            StudentId = null
        },
        new UniversityUser
        {
            Email = "student00128@dkit.ie",
            Password = "Student123!",
            Role = "Student",
            StudentId = 1
        },
        new UniversityUser
        {
            Email = "student12345@dkit.ie",
            Password = "Student123!",
            Role = "Student",
            StudentId = 2
        }
    };

    private const string JwtKey = "ca2_university_api_key_2025";
    private const string JwtIssuer = "UniversityAPI";

    // POST: api/auth/login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequestDto request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        var user = Users
            .SingleOrDefault(u =>
                string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase)
                && u.Password == request.Password);

        if (user == null)
        {
            return Unauthorized();
        }

        var token = GenerateJwtToken(user);

        var response = new LoginResponseDto
        {
            AccessToken = token,
            ExpiresIn = 3600,
            Role = user.Role,
            StudentId = user.StudentId
        };

        return Ok(response);
    }

    private string GenerateJwtToken(UniversityUser user)
    {
        // Convert secret key string into bytes that can be used for signing
        var keyBytes = Encoding.UTF8.GetBytes(JwtKey);

        // SymmetricSecurityKey is the key object used to sign and later vallidate the token
        var securityKey = new SymmetricSecurityKey(keyBytes);

        // SigningCredentials describes which key and algorithm are used to sign the token
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Claims describe who the user is and what they can do; they will be embedded into the token payload
        var claims = new List<Claim>
        {
            // sub is the main identifier of the user inside the token; here we use email
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),

            // Role claim is used by [Authorize(Roles)] to apply role-based authorization in controllers
            new Claim(ClaimTypes.Role, user.Role)
        };

        if (user.StudentId.HasValue)
        {
            // Custom "studentId" claim links this token to a specific Student row in the database
            claims.Add(new Claim("studentId", user.StudentId.Value.ToString()));
        }

        // JwtSecurityToken is the in-memory representation of the JWT with header, payload (claims) and expiry
        var token = new JwtSecurityToken(
            issuer: JwtIssuer, // Issuer must match the value used in JWT validation
            audience: JwtIssuer, // Audience usually matches issuer in simple APIs
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1), // Token will be valid for 1 hour from now
            signingCredentials: credentials // Attach signing info so the server can verify the token later
        );

        // WriteToken converts the token object into a compact "header.payload.signature" string sent to the client
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private class UniversityUser
    {
        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string Role { get; set; } = null!;

        public int? StudentId { get; set; }
    }
}
