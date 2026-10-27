using Calcusystem.Api.Http;
using Calcusystem.Api.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options => WireJson.Configure(options.JsonSerializerOptions));
builder.Services.ConfigureHttpJsonOptions(options => WireJson.Configure(options.SerializerOptions));

builder.Services.AddProblemDetails();

// The document the front-end's types are generated from. Served in development, and written to Api.json at
// build time so regenerating them needs no running server.
builder.Services.AddOpenApi(options => options.AddSchemaTransformer(WireSchema.Transform));

// No origin is allowed unless one is configured, so a deployment that says nothing serves no browser but its
// own. `appsettings.Development.json` names the local front-end.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddSingleton<DraftWorkflow>();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.MapControllers();

app.Run();

/// <summary>
/// Named so a test host can reach it. Top-level statements compile to an internal entry point, which
/// <c>WebApplicationFactory</c> cannot find.
/// </summary>
public partial class Program;
