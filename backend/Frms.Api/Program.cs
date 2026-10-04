using System.Text.Json;
using System.Text.Json.Serialization;
using Frms.Api.DependencyInjection;
using Frms.Api.Middleware;
using Frms.Business.DependencyInjection;
using Frms.DataAccess.DependencyInjection;
using Frms.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer((operation, _, _) =>
    {
        operation.Description =
            "Contract scaffold only. This operation returns 501 Not Implemented until its owning module is implemented.";
        return Task.CompletedTask;
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddBusiness();

var connectionString = builder.Configuration.GetConnectionString("FrmsDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'FrmsDatabase' must be configured. Use a secret store or environment variable for real credentials.");

builder.Services.AddDataAccess(connectionString);
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program;
