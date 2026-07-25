using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SeatFlow.Api.Errors;
using SeatFlow.Infrastructure;
using SeatFlow.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

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
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT settings are not configured.");

jwtOptions.Validate();

builder.Services.AddInfrastructure(
    databaseConnectionString,
    jwtOptions);

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        options =>
        {
            options.MapInboundClaims = false;

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

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet(
        "/",
        () => Results.Ok(
            new
            {
                application = "SeatFlow.Api",
                status = "Running"
            }))
    .WithName("GetApiStatus");

app.Run();

public partial class Program;