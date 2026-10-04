using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyStore.Api.Data;

namespace MyStore.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext db;
    public ProfileController(AppDbContext db) => this.db = db;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == UserId);
        if (u is null) return NotFound(new { message = "User not found." });
        return Ok(new { u.Id, u.Name, u.Email, u.Role });
    }

    [HttpPut]
    public async Task<IActionResult> Update(ProfileRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) return BadRequest(new { message = "Name is required." });
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == UserId);
        if (u is null) return NotFound(new { message = "User not found." });
        u.Name = r.Name.Trim();
        await db.SaveChangesAsync();
        return Ok(new { u.Id, u.Name, u.Email, u.Role });
    }

    public record ProfileRequest(string Name);
}
