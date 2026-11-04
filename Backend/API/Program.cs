using System.Text.Json.Serialization;
using Repositories;
using Services;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddRepositories(
    builder.Configuration.GetConnectionString("DefaultConnection")!);
builder.Services.AddServices();
builder.Services.AddExceptionHandler<API.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o => o.AddPolicy("Angular", p => p
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/dev/seed", async (DbSeeder seeder, int? count, bool? reset, CancellationToken ct) =>
    {
        var inserted = await seeder.SeedAsync(count ?? 100_000, reset ?? false, ct);
        return Results.Ok(new { inserted });
    });
}



app.UseHttpsRedirection();

app.UseCors("Angular");

app.UseAuthorization();

app.MapControllers();

app.Run();
public partial class Program { }
