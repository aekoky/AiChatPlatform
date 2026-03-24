namespace AiService.Infrastructure.Options;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string InstructModel { get; set; } = string.Empty;

    public string EmbeddingModel { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 300;
}
