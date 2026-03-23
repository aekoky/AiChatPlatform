using DocumentIngestion.Infrastructure;
using Wolverine;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddIngestionInfrastructure(builder.Configuration);

builder.UseWolverine(opts =>
{
    opts.ConfigureWolverine(builder.Configuration);
});

var app = builder.Build();
app.Run();