using System.Text.Json.Serialization;
using Calcusystem.Api.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Property names as written, matching what a saved document looks like on disk. A camelCase policy here
        // would give the same DTO two spellings, and a client could not save what it was served.
        options.JsonSerializerOptions.PropertyNamingPolicy = null;

        // Enum members as names. Numbers would renumber an existing payload's meaning the moment a member is
        // inserted, and a name is what a front-end can put in a menu.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // A draft is mostly unset, and absent already means "not supplied yet".
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddProblemDetails();

// No origin is allowed unless one is configured, so a deployment that says nothing serves no browser but its
// own. `appsettings.Development.json` names the local front-end.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddSingleton<DraftWorkflow>();

var app = builder.Build();

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
