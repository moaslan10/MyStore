using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyStore.Api.Data;
using MyStore.Api.Models;
using MyStore.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<JwtService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MyStore.Api";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalException");
        if (feature?.Error is not null) logger.LogError(feature.Error, "Unhandled API error");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var message = app.Environment.IsDevelopment() && feature?.Error is not null
            ? feature.Error.Message
            : "Server error while processing the request.";
        await context.Response.WriteAsJsonAsync(new { message });
    });
});

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "MyStore API", time = DateTime.UtcNow }));
app.MapGet("/", () => Results.Ok(new { service = "MyStore API", swagger = "/swagger", health = "/api/health" }));
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    // Upgrade older demo databases without issuing duplicate ALTER TABLE statements.
    // SQLite logs a failed command before throwing when a column already exists, so
    // inspect PRAGMA table_info first and only add columns that are actually missing.
    async Task<bool> ColumnExistsAsync(string table, string column)
    {
        await using var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table.Replace("\"", "\"\"")}\")";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    async Task AddColumnIfMissing(string table, string column, string definition)
    {
        if (!await ColumnExistsAsync(table, column))
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition}");
    }

    await AddColumnIfMissing("Users", "PasswordHash", "TEXT NOT NULL DEFAULT ''");
    await AddColumnIfMissing("Users", "RefreshTokenHash", "TEXT NULL");
    await AddColumnIfMissing("Users", "RefreshTokenExpiresAt", "TEXT NULL");

    if (!await db.Users.AnyAsync())
    {
        db.Users.Add(new User
        {
            Email = "admin@mystore.com",
            PasswordHash = PasswordService.Hash("123456"),
            Name = "Store Admin",
            Role = "Admin"
        });
    }

    if (!await db.Products.AnyAsync())
    {
        db.Products.AddRange(
            new Product { Title = "Wireless Headphones", Description = "Comfortable wireless headphones.", Price = 129.99m, Category = "electronics", Thumbnail = "/products/headphones.svg", Stock = 25, Rating = 4.8m, IsFeatured = true },
            new Product { Title = "Smart Watch", Description = "Modern smart watch.", Price = 199.99m, Category = "electronics", Thumbnail = "/products/watch.svg", Stock = 18, Rating = 4.7m, IsFeatured = true },
            new Product { Title = "Laptop", Description = "Powerful laptop.", Price = 899.99m, Category = "laptops", Thumbnail = "/products/laptop.svg", Stock = 10, Rating = 4.9m, IsFeatured = true },
            new Product { Title = "Leather Bag", Description = "Classic everyday bag.", Price = 79.99m, Category = "bags", Thumbnail = "/products/bag.svg", Stock = 30, Rating = 4.6m },
            new Product { Title = "Essence Mascara", Description = "Long-lasting mascara.", Price = 9.99m, Category = "beauty", Thumbnail = "/products/mascara.svg", Stock = 40, Rating = 4.5m }
        );
    }

    await db.SaveChangesAsync();

    // Create feature tables when upgrading an existing SQLite database.
    await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS WishlistItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, ProductId INTEGER NOT NULL, CreatedAt TEXT NOT NULL, UNIQUE(UserId, ProductId))");
    await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS Reviews (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, ProductId INTEGER NOT NULL, Rating INTEGER NOT NULL, Comment TEXT NOT NULL, CreatedAt TEXT NOT NULL, UNIQUE(UserId, ProductId))");
}

app.Run();
