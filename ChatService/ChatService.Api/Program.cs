using Wolverine;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ChatService.Application.Features.StartChat;
using ChatService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Keycloak Authentication Configuration
var keycloakAuthority = builder.Configuration["Keycloak:Authority"] ?? throw new InvalidOperationException("Keycloak:Authority is missing.");
var keycloakAudience = builder.Configuration["Keycloak:Audience"] ?? throw new InvalidOperationException("Keycloak:Audience is missing.");

// Add services to the container.
builder.Services.AddControllers();

// OpenAPI configuration with OAuth2 token support via Transformer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "AiChatPlatform API",
            Version = "v1"
        };

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/auth"),
                    TokenUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/token"),
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
        options.Authority = keycloakAuthority;
        options.RequireHttpsMetadata = false; // Dev only
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = keycloakAudience
        };
    });

// Map Marten from Configuration
var martenDbConn = builder.Configuration.GetConnectionString("Marten") ?? throw new InvalidOperationException("Marten ConnectionString is missing.");
builder.Services.ConfigureWolverineMarten(martenDbConn);

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(StartChatCommand).Assembly);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "OpenAPI V1");
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
