using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Frms.Api.Authentication;
using Frms.Api.BackgroundJobs;
using Frms.Api.Configuration;
using Frms.Api.DTOs.Responses;
using Frms.Api.Middleware;
using Frms.Business.Abstractions.Security;
using Frms.Business.DependencyInjection;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.DependencyInjection;
using Frms.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddDevelopmentEnvFile(builder.Environment);
builder.Services.AddDataAccess(builder.Configuration);
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
{
    var errors = context.ModelState.Where(pair => pair.Value?.Errors.Count > 0).ToDictionary(pair => ToCamelCase(pair.Key), pair => pair.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
    return new BadRequestObjectResult(new ApiErrorResponse("VALIDATION_ERROR", "Request validation failed.", context.HttpContext.TraceIdentifier, errors));
});
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddOptions<BCryptOptions>().Bind(builder.Configuration.GetSection(BCryptOptions.SectionName)).Validate(options => options.WorkFactor is >= 4 and <= 31).ValidateOnStart();
builder.Services.AddOptions<JwtOptions>().Bind(builder.Configuration.GetSection(JwtOptions.SectionName)).Validate(options => !string.IsNullOrWhiteSpace(options.Issuer)).Validate(options => !string.IsNullOrWhiteSpace(options.Audience)).Validate(options => Encoding.UTF8.GetByteCount(options.SigningKey ?? string.Empty) >= 32).Validate(options => options.LifetimeMinutes > 0).ValidateOnStart();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = jwt.Issuer, ValidateAudience = true, ValidAudience = jwt.Audience, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)), ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30), NameClaimType = JwtRegisteredClaimNames.Sub, RoleClaimType = ClaimTypes.Role };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var subject = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
            if (!Guid.TryParse(subject, out var accountId) || string.IsNullOrWhiteSpace(role)) { context.Fail("Invalid subject or role claim."); return; }
            var service = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationService>();
            if (!await service.IsActiveAsync(accountId, role, context.HttpContext.RequestAborted)) context.Fail("Account is no longer active or its role changed.");
        },
        OnChallenge = async context => { context.HandleResponse(); context.Response.StatusCode = 401; await context.Response.WriteAsJsonAsync(new ApiErrorResponse("UNAUTHORIZED", "Authentication is required.", context.HttpContext.TraceIdentifier)); },
        OnForbidden = async context => { context.Response.StatusCode = 403; await context.Response.WriteAsJsonAsync(new ApiErrorResponse("FORBIDDEN", "The authenticated account is not authorized for this action.", context.HttpContext.TraceIdentifier)); }
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CUSTOMER", policy => policy.RequireRole("CUSTOMER"))
    .AddPolicy("FACILITY_STAFF", policy => policy.RequireRole("FACILITY_STAFF"))
    .AddPolicy("FACILITY_MANAGER", policy => policy.RequireRole("FACILITY_MANAGER"))
    .AddPolicy("BUSINESS_OPERATIONS_MANAGER", policy => policy.RequireRole("BUSINESS_OPERATIONS_MANAGER"))
    .AddPolicy("SYSTEM_ADMINISTRATOR", policy => policy.RequireRole("SYSTEM_ADMINISTRATOR"));
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddBusiness();
builder.Services.AddInfrastructure();
builder.Services.AddFrmsBackgroundJobs();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.HasStarted || response.ContentLength is > 0) return;
    var (code, message) = response.StatusCode switch { 404 => ("RESOURCE_NOT_FOUND", "The requested resource was not found."), 405 => ("METHOD_NOT_ALLOWED", "The HTTP method is not allowed for this resource."), _ => ("HTTP_ERROR", "The request could not be completed.") };
    await response.WriteAsJsonAsync(new ApiErrorResponse(code, message, context.HttpContext.TraceIdentifier));
});
app.MapOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.Run();

static string ToCamelCase(string value) => string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
public partial class Program;
