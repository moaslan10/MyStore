using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyStore.Api.Data;
using MyStore.Api.Models;

namespace MyStore.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext db;
    public ReviewsController(AppDbContext db) => this.db = db;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("product/{productId:int}")]
    public async Task<IActionResult> ProductReviews(int productId)
    {
        var reviews = await db.Reviews.AsNoTracking().Where(x => x.ProductId == productId).OrderByDescending(x => x.CreatedAt).ToListAsync();
        var userIds = reviews.Select(x => x.UserId).Distinct().ToList();
        var users = await db.Users.AsNoTracking().Where(x => userIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        return Ok(reviews.Select(x => new { x.Id, x.ProductId, x.Rating, x.Comment, x.CreatedAt, userName = users.GetValueOrDefault(x.UserId, "Customer") }));
    }

    [Authorize]
    [HttpPost("product/{productId:int}")]
    public async Task<IActionResult> Add(int productId, ReviewRequest r)
    {
        if (r.Rating < 1 || r.Rating > 5) return BadRequest(new { message = "Rating must be between 1 and 5." });
        if (!await db.Products.AnyAsync(x => x.Id == productId)) return NotFound(new { message = "Product not found." });
        var review = await db.Reviews.FirstOrDefaultAsync(x => x.ProductId == productId && x.UserId == UserId);
        if (review is null)
        {
            review = new Review { ProductId = productId, UserId = UserId, Rating = r.Rating, Comment = r.Comment?.Trim() ?? "" };
            db.Reviews.Add(review);
        }
        else { review.Rating = r.Rating; review.Comment = r.Comment?.Trim() ?? ""; review.CreatedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync();
        return Ok(new { message = "Review saved." });
    }

    public record ReviewRequest(int Rating, string? Comment);
}
