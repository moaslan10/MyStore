using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyStore.Api.Data;
using MyStore.Api.Models;

namespace MyStore.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext db;
    public OrdersController(AppDbContext db) => this.db = db;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // IMPORTANT: Never return EF entities directly from an API response here.
    // Order -> Items -> Order creates a JSON reference cycle.
    private static OrderResponse ToResponse(Order order) => new(
        order.Id,
        order.UserId,
        order.Total,
        order.PaymentMethod,
        order.Status,
        order.FullName,
        order.Phone,
        order.Address,
        order.City,
        order.CreatedAt,
        order.Items.Select(i => new OrderItemResponse(
            i.Id,
            i.ProductId,
            i.ProductTitle,
            i.UnitPrice,
            i.Quantity
        )).ToList()
    );

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.FullName) || string.IsNullOrWhiteSpace(r.Phone) ||
            string.IsNullOrWhiteSpace(r.Address) || string.IsNullOrWhiteSpace(r.City))
            return BadRequest(new { message = "Please complete all delivery fields." });

        if (r.Items is null || r.Items.Count == 0)
            return BadRequest(new { message = "Cart is empty." });

        var payment = (r.PaymentMethod ?? "cash").Trim().ToLowerInvariant();
        if (payment is not ("cash" or "card"))
            return BadRequest(new { message = "Invalid payment method." });

        // Combine duplicate product lines before checking stock/deducting it.
        var requested = r.Items
            .GroupBy(x => x.ProductId)
            .Select(g => new OrderItemRequest(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        if (requested.Any(x => x.Quantity < 1))
            return BadRequest(new { message = "Quantity must be at least 1." });

        var ids = requested.Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products.Where(x => ids.Contains(x.Id)).ToListAsync();
        if (products.Count != ids.Count)
            return BadRequest(new { message = "One or more products no longer exist." });

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            decimal total = 0;
            var items = new List<OrderItem>();

            foreach (var line in requested)
            {
                var product = products.First(x => x.Id == line.ProductId);

                if (line.Quantity > product.Stock)
                    return BadRequest(new { message = $"Not enough stock for {product.Title}. Available: {product.Stock}." });

                product.Stock -= line.Quantity;
                total += product.Price * line.Quantity;

                items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductTitle = product.Title,
                    UnitPrice = product.Price,
                    Quantity = line.Quantity
                });
            }

            var order = new Order
            {
                UserId = UserId,
                Total = total,
                PaymentMethod = payment,
                Status = "Pending",
                FullName = r.FullName.Trim(),
                Phone = r.Phone.Trim(),
                Address = r.Address.Trim(),
                City = r.City.Trim(),
                Items = items
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            // Reload with Items, then map to a cycle-free DTO.
            var saved = await db.Orders
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstAsync(x => x.Id == order.Id);

            return Ok(ToResponse(saved));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new
            {
                message = "Could not create the order.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var orders = await db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.UserId == UserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(ToResponse));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> All()
    {
        var orders = await db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(ToResponse));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> Status(int id, [FromBody] string status)
    {
        var allowed = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
        if (!allowed.Contains(status))
            return BadRequest(new { message = "Invalid order status." });

        var order = await db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order is null)
            return NotFound(new { message = "Order not found." });

        order.Status = status;
        await db.SaveChangesAsync();
        return Ok(ToResponse(order));
    }

    private sealed record OrderResponse(
        int Id,
        int UserId,
        decimal Total,
        string PaymentMethod,
        string Status,
        string FullName,
        string Phone,
        string Address,
        string City,
        DateTime CreatedAt,
        List<OrderItemResponse> Items
    );

    private sealed record OrderItemResponse(
        int Id,
        int ProductId,
        string ProductTitle,
        decimal UnitPrice,
        int Quantity
    );
}
