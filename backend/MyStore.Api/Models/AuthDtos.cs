namespace MyStore.Api.Models;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Name, string Email, string Password);
public record UserDto(int Id, string Email, string Name, string Role);
public record LoginResponse(string Token, string RefreshToken, UserDto User);
public record RefreshRequest(string RefreshToken);
