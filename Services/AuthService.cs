using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Persona.Data;
using Persona.Models;

namespace Persona.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(string username, string password)
    {
        if (await _db.Users.AnyAsync(u => u.Username == username))
            throw new InvalidOperationException("用户名已存在");

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsGuest = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponse(GenerateToken(user), username, false);
    }

    public async Task<AuthResponse> LoginAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username)
            ?? throw new InvalidOperationException("用户名或密码错误");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new InvalidOperationException("用户名或密码错误");

        return new AuthResponse(GenerateToken(user), username, false);
    }

    public async Task<AuthResponse> GuestLoginAsync()
    {
        var guestName = $"游客{DateTime.UtcNow.Ticks % 100000}";
        var user = new User
        {
            Username = guestName,
            PasswordHash = "",
            IsGuest = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponse(GenerateToken(user), guestName, true);
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("is_guest", user.IsGuest.ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
