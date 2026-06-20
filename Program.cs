using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Persona.Components;
using Persona.Data;
using Persona.Models;
using Persona.Services;

var builder = WebApplication.CreateBuilder(args);

// Config
builder.Services.Configure<DeepSeekOptions>(builder.Configuration.GetSection("DeepSeek"));

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=persona.db"));

// JWT Auth
var jwtKey = builder.Configuration["Jwt:SecretKey"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<ReportService>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// DB init + admin seed
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

// Auth API endpoints
app.MapPost("/api/auth/register", async (RegisterRequest req, AuthService auth) =>
{
    try { return Results.Ok(await auth.RegisterAsync(req.Username, req.Password)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPost("/api/auth/login", async (LoginRequest req, AuthService auth) =>
{
    try { return Results.Ok(await auth.LoginAsync(req.Username, req.Password)); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPost("/api/auth/guest", async (AuthService auth) =>
    Results.Ok(await auth.GuestLoginAsync()));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
