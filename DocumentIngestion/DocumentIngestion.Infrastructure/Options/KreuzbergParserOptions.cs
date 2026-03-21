namespace DocumentIngestion.Infrastructure.Options;

public class KreuzbergParserOptions
{
    public const string SectionName = "KreuzbergParser";

    public bool UseCache { get; set; } = true;
    public bool EnableQualityProcessing { get; set; } = true;

    // Chunking
    public bool EnableChunking { get; set; } = true;
    public int ChunkMaxChars { get; set; } = 2000;
    public int ChunkOverlap { get; set; } = 200;

    // OCR
    public bool EnableOcr { get; set; } = false;
    public string OcrBackend { get; set; } = "tesseract";
    public string OcrLanguage { get; set; } = "eng";

    // Language detection
    public bool EnableLanguageDetection { get; set; } = false;
    public float LanguageMinConfidence { get; set; } = 0.8f;
}