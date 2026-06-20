using System.ComponentModel.DataAnnotations;

namespace Persona.Models;

// === Auth ===
public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Password);
public record AuthResponse(string Token, string Username, bool IsGuest);

// === Quiz ===
public record DimensionScores(
    int O, double OConf,
    int C, double CConf,
    int E, double EConf,
    int A, double AConf,
    int N, double NConf);

public record ReportResponse(
    int Openness, int Conscientiousness, int Extraversion,
    int Agreeableness, int Neuroticism, string Summary, string MbtiType);

// === EF Entities ===
public class User
{
    public int Id { get; set; }
    [MaxLength(50)] public string Username { get; set; } = "";
    [MaxLength(200)] public string PasswordHash { get; set; } = "";
    public bool IsGuest { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<QuizSession> Sessions { get; set; } = new();
}

public class QuizSession
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Completed { get; set; }
    public string? ScoresJson { get; set; }
    public string? ReportJson { get; set; }
    public string? UsedAnglesJson { get; set; } // JSON array of "dim:topic"
    public int ScoreRounds { get; set; }
    public List<QuizMessage> Messages { get; set; } = new();
}

public class QuizMessage
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    [MaxLength(20)] public string Role { get; set; } = ""; // "user" / "assistant"
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public QuizSession Session { get; set; } = null!;
}
