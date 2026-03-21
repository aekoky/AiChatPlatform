namespace NotificationService.Api.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string Uri { get; set; } = string.Empty;
}
