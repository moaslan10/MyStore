using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyStore.Api.Data;

namespace MyStore.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext db;
    public AdminController(AppDbContext db) => this.db = db;

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var orders = db.Orders.AsNoTracking();
        var products = db.Products.AsNoTracking();
        var users = db.Users.AsNoTracking();

        // SQLite's EF Core provider cannot translate Sum() over decimal.
        // Load only the required totals and aggregate in .NET instead.
        var revenueValues = await orders
            .Where(x => x.Status != "Cancelled")
            .Select(x => x.Total)
            .ToListAsync();
        var revenue = revenueValues.Sum();

        return Ok(new
        {
            products = await products.CountAsync(),
            customers = await users.CountAsync(x => x.Role == "Customer"),
            orders = await orders.CountAsync(),
            pendingOrders = await orders.CountAsync(x => x.Status == "Pending"),
            processingOrders = await orders.CountAsync(x => x.Status == "Processing"),
            deliveredOrders = await orders.CountAsync(x => x.Status == "Delivered"),
            lowStock = await products.CountAsync(x => x.Stock <= 5),
            revenue
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users()
    {
        var result = await db.Users.AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Select(x => new { x.Id, x.Name, x.Email, x.Role })
            .ToListAsync();
        return Ok(result);
    }
}
