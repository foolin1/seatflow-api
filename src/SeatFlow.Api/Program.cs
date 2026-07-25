using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using SeatFlow.Api.BackgroundServices;
using SeatFlow.Api.Errors;
using SeatFlow.Api.Health;
using SeatFlow.Infrastructure;
using SeatFlow.Infrastructure.Authentication;
using SeatFlow.Infrastructure.Persistence;

var builder =
    WebApplication.CreateBuilder(
        args);

var databaseConnectionString =
    builder.Configuration.GetConnectionString(
        "Database");

if (string.IsNullOrWhiteSpace(
        databaseConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Database' is not configured. " +
        "Configure it through .NET User Secrets or " +
        "the ConnectionStrings__Database environment variable.");
}

var jwtOptions =
    builder.Configuration
        .GetSection(
            JwtOptions.SectionName)
        .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT settings are not configured.");

jwtOptions.Validate();

builder.Services.AddInfrastructure(
    databaseConnectionString,
    jwtOptions);

builder.Services.AddHostedService<
    ReservationExpirationWorker>();

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () =>
            HealthCheckResult.Healthy(
                "SeatFlow API is running."),
        tags:
            new[]
            {
                "live"
            })
    .AddCheck<DatabaseHealthCheck>(
        "database",
        tags:
            new[]
            {
                "ready"
            });

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        options =>
        {
            options.MapInboundClaims =
                false;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer =
                        jwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience =
                        jwtOptions.Audience,

                    ValidateLifetime = true,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                jwtOptions.SigningKey)),

                    NameClaimType = "name",
                    RoleClaimType = "role",

                    ClockSkew =
                        TimeSpan.FromSeconds(30)
                };
        });

builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<
    ApiExceptionHandler>();

builder.Services.AddProblemDetails();

builder.Services
    .AddControllers()
    .AddJsonOptions(
        options =>
        {
            options.JsonSerializerOptions
                .Converters
                .Add(
                    new JsonStringEnumConverter());
        });

builder.Services.AddOpenApi();

var app =
    builder.Build();

await using (
    var migrationScope =
        app.Services.CreateAsyncScope())
{
    var dbContext =
        migrationScope.ServiceProvider
            .GetRequiredService<
                SeatFlowDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate =
            registration =>
                registration.Tags.Contains(
                    "live"),

        ResponseWriter =
            HealthCheckResponseWriter.WriteAsync
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate =
            registration =>
                registration.Tags.Contains(
                    "ready"),

        ResponseWriter =
            HealthCheckResponseWriter.WriteAsync
    });

app.MapGet(
        "/",
        () =>
            Results.Ok(
                new
                {
                    application =
                        "SeatFlow.Api",
                    status =
                        "Running"
                }))
    .WithName(
        "GetApiStatus");

app.Run();

public partial class Program;