namespace Persona.Services;

public class SessionState
{
    public string? Token { get; set; }
    public string? Username { get; set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
}
