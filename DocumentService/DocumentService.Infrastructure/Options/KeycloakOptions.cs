namespace DocumentService.Infrastructure.Options;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;
}
