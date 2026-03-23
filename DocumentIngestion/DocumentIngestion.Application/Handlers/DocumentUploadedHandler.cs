using BuildingBlocks.Contracts.DocumentEvents;
using BuildingBlocks.Contracts.ValueObjects;
using DocumentIngestion.Application.Exceptions;
using DocumentIngestion.Application.Services;
using Wolverine;

namespace DocumentIngestion.Application.Handlers;

public class DocumentUploadedHandler(
    IChunkingService chunker,
    IEmbeddingService embeddingService,
    IVectorStoreRepository vectorStore,
    IStorageService storage)
{
    public async Task Handle(
        DocumentUploadedEvent message,
        IMessageContext context,
        CancellationToken ct)
    {
        try
        {
            var s3Key = message.DocumentId.ToString();
            await using var stream = await storage.DownloadAsync(s3Key, ct);

            var chunks = await chunker.ParseAndChunkAsync(
                stream, message.ContentType, message.FileName, message.DocumentId, ct);

            var embeddings = await embeddingService.EmbedAsync(
                [.. chunks.Select(c => c.Content)], ct);

            for (var i = 0; i < chunks.Count; i++)
                chunks[i].Embedding = embeddings[i];

            await vectorStore.SaveChunksAsync(
                chunks, message.UserId, message.SessionId, message.Scope, ct);

            await context.PublishAsync(new DocumentIndexedEvent(
                DocumentId: message.DocumentId,
                UserId: message.UserId,
                ChunkCount: chunks.Count));
        }
        catch (InvalidDocumentFormatException)
        {
            await context.PublishAsync(new DocumentIndexingFailedEvent(
                DocumentId: message.DocumentId,
                UserId: message.UserId,
                Reason: DocumentFailedReasons.InvalidFormat));
        }
        catch (DocumentParsingFailedException)
        {
            await context.PublishAsync(new DocumentIndexingFailedEvent(
                DocumentId: message.DocumentId,
                UserId: message.UserId,
                Reason: DocumentFailedReasons.ParsingFailed));
        }
        catch (StorageException)
        {
            await context.PublishAsync(new DocumentIndexingFailedEvent(
                DocumentId: message.DocumentId,
                UserId: message.UserId,
                Reason: DocumentFailedReasons.StorageError));
        }
        catch (Exception)
        {
            await context.PublishAsync(new DocumentIndexingFailedEvent(
                DocumentId: message.DocumentId,
                UserId: message.UserId,
                Reason: DocumentFailedReasons.Unknown));
        }
    }
}