using DocumentIngestion.Application.Domain;
using DocumentIngestion.Application.Exceptions;
using DocumentIngestion.Application.Services;
using DocumentIngestion.Infrastructure.Options;
using Kreuzberg;
using Microsoft.Extensions.Options;

namespace DocumentIngestion.Infrastructure.Parsers;

public class KreuzbergDocumentParser(IOptions<KreuzbergParserOptions> options) : IChunkingService
{
    private readonly KreuzbergParserOptions _options = options.Value;

    public async Task<List<DocumentChunk>> ParseAndChunkAsync(
        Stream stream,
        string contentType,
        string fileName,
        Guid documentId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (string.IsNullOrEmpty(contentType)) throw new ArgumentException("Content type is required.", nameof(contentType));

        if (stream.CanSeek) stream.Position = 0;

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);
        var bytes = memoryStream.ToArray();

        var config = new ExtractionConfig
        {
            UseCache = _options.UseCache,
            EnableQualityProcessing = _options.EnableQualityProcessing,

            Chunking = new ChunkingConfig
            {
                MaxChars = _options.ChunkMaxChars,
                MaxOverlap = _options.ChunkOverlap
            },

            Ocr = _options.EnableOcr
                ? new OcrConfig
                {
                    Backend = _options.OcrBackend,
                    Language = _options.OcrLanguage
                }
                : null,

            LanguageDetection = _options.EnableLanguageDetection
                ? new LanguageDetectionConfig
                {
                    Enabled = true,
                    MinConfidence = _options.LanguageMinConfidence
                }
                : null
        };

        try
        {
            var result = await KreuzbergClient.ExtractBytesAsync(bytes, contentType, config, ct);

            if (result.Chunks is null || result.Chunks.Count == 0)
                return [];

            return [.. result.Chunks
                .Select(chunk => new DocumentChunk
                {
                    DocumentId = documentId,
                Content = chunk.Content,
                    ChunkIndex = chunk.Metadata.ChunkIndex,
                    Metadata = new DocumentChunkMetadata
                    {
                        FileName = fileName,
                        SourceType = contentType,
                        PageNumber = chunk.Metadata.FirstPage
                    }
                })];
        }
        catch (KreuzbergValidationException ex)
        {
            throw new InvalidDocumentFormatException(ex.Message, ex);
        }
        catch (KreuzbergParsingException ex)
        {
            throw new DocumentParsingFailedException(ex.Message, ex);
        }
        catch (KreuzbergMissingDependencyException ex)
        {
            throw new DocumentParsingFailedException(ex.Message, ex);
        }
    }    
}