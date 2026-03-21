using ChatService.Application.Services;
using ChatService.Infrastructure;
using ChatService.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

// Keycloak Authentication Configuration
builder.Services.Configure<OpenApiOptions>(builder.Configuration.GetSection(OpenApiOptions.SectionName));
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

var keycloakOptions = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>()
    ?? throw new InvalidOperationException("Keycloak options are missing.");
var openApiOptions = builder.Configuration.GetSection(OpenApiOptions.SectionName).Get<OpenApiOptions>()
    ?? throw new InvalidOperationException("OpenApi options are missing.");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();

// OpenAPI configuration with OAuth2 token support via Transformer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers =
        [
            new OpenApiServer { Url = openApiOptions.ServerUrl }
        ];
        document.Info = new OpenApiInfo
        {
            Title = "AiChatPlatform Chat API",
            Version = "v1"
        };

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{keycloakOptions.Authority}/protocol/openid-connect/auth"),
                    TokenUrl = new Uri($"{keycloakOptions.Authority}/protocol/openid-connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenId" },
                        { "profile", "Profile" },
                        { "email", "Email" }
                    }
                }
            }
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["OAuth2"] = scheme;

        document.Security = [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("OAuth2"),
                    ["api", "profile", "email", "openid"]
                }
            }
        ];

        // Set the host document for all elements
        // including the security scheme references
        document.SetReferenceHostDocument();

        return Task.CompletedTask;
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakOptions.Authority;
        options.RequireHttpsMetadata = false; // Dev only
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = keycloakOptions.Audience
        };
    });

// Map Marten from Configuration
var martenDbConn = builder.Configuration.GetConnectionString("Marten") ?? throw new InvalidOperationException("Marten ConnectionString is missing.");
builder.Services.ConfigureWolverineMarten(martenDbConn);

builder.Host.UseWolverine(opts =>
{
    opts.ConfigureWolverine(builder.Services);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/chat/openapi/v1.json", "OpenAPI V1");
        options.OAuthClientId("aichat-web");
        options.OAuthClientSecret(string.Empty);
        options.OAuthUsePkce();
        options.OAuthScopes("openid", "profile", "email");
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
