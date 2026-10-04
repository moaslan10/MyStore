namespace MyStore.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    // Kept only for backward compatibility with old demo databases. New passwords use PasswordHash.
    public string? Password { get; set; }
    public string PasswordHash { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Customer";
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public List<Order> Orders { get; set; } = new();
}
