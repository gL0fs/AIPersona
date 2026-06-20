namespace Persona.Services;

public class DeepSeekOptions
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.deepseek.com/v1";
    public string ModelName { get; set; } = "deepseek-chat";
}
