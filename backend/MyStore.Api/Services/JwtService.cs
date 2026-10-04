using Microsoft.IdentityModel.Tokens;
using MyStore.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MyStore.Api.Services;

public class JwtService
{
    private readonly IConfiguration c;
    public JwtService(IConfiguration c) => this.c = c;

    public string CreateToken(User u)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Email, u.Email),
            new Claim(ClaimTypes.Name, u.Name),
            new Claim(ClaimTypes.Role, u.Role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(c["Jwt:Key"]!));
        var minutes = c.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 15;
        var t = new JwtSecurityToken(c["Jwt:Issuer"], c["Jwt:Issuer"], claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(t);
    }

    public string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public DateTime RefreshExpiryUtc => DateTime.UtcNow.AddDays(c.GetValue<int?>("Jwt:RefreshTokenDays") ?? 7);
}
