using SeatFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var databaseConnectionString =
    builder.Configuration.GetConnectionString("Database");

if (string.IsNullOrWhiteSpace(databaseConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Database' is not configured. " +
        "Configure it through .NET User Secrets or " +
        "the ConnectionStrings__Database environment variable.");
}

builder.Services.AddInfrastructure(
    databaseConnectionString);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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