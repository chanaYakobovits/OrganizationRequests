using Repositories;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();



builder.Services.AddRepositories(
    builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
