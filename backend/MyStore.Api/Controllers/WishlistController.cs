using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyStore.Api.Data;
using MyStore.Api.Models;

namespace MyStore.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly AppDbContext db;
    public WishlistController(AppDbContext db) => this.db = db;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var ids = await db.WishlistItems.AsNoTracking().Where(x => x.UserId == UserId).Select(x => x.ProductId).ToListAsync();
        var products = await db.Products.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync();
        return Ok(products.OrderBy(x => ids.IndexOf(x.Id)));
    }

    [HttpPost("{productId:int}")]
    public async Task<IActionResult> Add(int productId)
    {
        if (!await db.Products.AnyAsync(x => x.Id == productId)) return NotFound(new { message = "Product not found." });
        if (!await db.WishlistItems.AnyAsync(x => x.UserId == UserId && x.ProductId == productId))
        {
            db.WishlistItems.Add(new WishlistItem { UserId = UserId, ProductId = productId });
            await db.SaveChangesAsync();
        }
        return Ok(new { message = "Added to wishlist." });
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Remove(int productId)
    {
        var item = await db.WishlistItems.FirstOrDefaultAsync(x => x.UserId == UserId && x.ProductId == productId);
        if (item is not null) { db.WishlistItems.Remove(item); await db.SaveChangesAsync(); }
        return NoContent();
    }
}
