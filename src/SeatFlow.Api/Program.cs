var builder = WebApplication.CreateBuilder(args);

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