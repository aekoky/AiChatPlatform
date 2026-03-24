namespace ChatService.Application.Dtos
{
    public record ConversationMetadataDto
    {
        public long SessionVersion { get; set; }
        public long MessageVersion { get; set; }
    }
}
