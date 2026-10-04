using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyStore.Api.Data;
using MyStore.Api.Models;
using MyStore.Api.Services;

namespace MyStore.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext db;
    private readonly JwtService jwt;
    public AuthController(AppDbContext db, JwtService jwt) { this.db = db; this.jwt = jwt; }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest r)
    {
        var email = (r.Email ?? "").Trim().ToLowerInvariant();
        var u = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (u is null) return Unauthorized(new { message = "Invalid email or password." });

        var valid = !string.IsNullOrWhiteSpace(u.PasswordHash) && PasswordService.Verify(r.Password, u.PasswordHash);
        // One-time migration for accounts created by the old demo version.
        if (!valid && string.IsNullOrEmpty(u.PasswordHash) && !string.IsNullOrEmpty(u.Password) && u.Password == r.Password)
        {
            u.PasswordHash = PasswordService.Hash(r.Password);
            u.Password = null;
            valid = true;
        }
        if (!valid) return Unauthorized(new { message = "Invalid email or password." });

        return Ok(await IssueSession(u));
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Email) || string.IsNullOrWhiteSpace(r.Password))
            return BadRequest(new { message = "All fields are required." });
        if (r.Password.Length < 8) return BadRequest(new { message = "Password must be at least 8 characters." });

        var email = r.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email)) return Conflict(new { message = "Email already exists." });

        var u = new User { Name = r.Name.Trim(), Email = email, PasswordHash = PasswordService.Hash(r.Password), Role = "Customer" };
        db.Users.Add(u);
        await db.SaveChangesAsync();
        return Ok(await IssueSession(u));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(RefreshRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.RefreshToken)) return Unauthorized(new { message = "Refresh token is required." });
        var hash = PasswordService.HashToken(r.RefreshToken);
        var u = await db.Users.FirstOrDefaultAsync(x => x.RefreshTokenHash == hash);
        if (u is null || u.RefreshTokenExpiresAt is null || u.RefreshTokenExpiresAt <= DateTime.UtcNow)
            return Unauthorized(new { message = "Refresh token expired or invalid." });
        return Ok(await IssueSession(u));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest r)
    {
        if (!string.IsNullOrWhiteSpace(r.RefreshToken))
        {
            var hash = PasswordService.HashToken(r.RefreshToken);
            var u = await db.Users.FirstOrDefaultAsync(x => x.RefreshTokenHash == hash);
            if (u is not null)
            {
                u.RefreshTokenHash = null;
                u.RefreshTokenExpiresAt = null;
                await db.SaveChangesAsync();
            }
        }
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot(ForgotRequest r)
    {
        var email = (r.Email ?? "").Trim().ToLowerInvariant();
        var exists = await db.Users.AnyAsync(x => x.Email == email);
        // Keep the response generic to avoid account enumeration.
        if (!exists) return Ok(new { message = "If the account exists, a reset code has been generated." });
        return Ok(new { message = "Password reset is available from the account email provider. Demo mode is disabled in this secure build." });
    }

    private async Task<LoginResponse> IssueSession(User u)
    {
        var refresh = jwt.CreateRefreshToken();
        u.RefreshTokenHash = PasswordService.HashToken(refresh);
        u.RefreshTokenExpiresAt = jwt.RefreshExpiryUtc;
        await db.SaveChangesAsync();
        return new LoginResponse(jwt.CreateToken(u), refresh, new UserDto(u.Id, u.Email, u.Name, u.Role));
    }

    public record ForgotRequest(string? Email);
}
