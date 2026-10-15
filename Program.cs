using InboxIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using InboxIntelligence.Mappings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registering dbcontext
builder.Services.AddDbContext<InboxIntelligenceDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("InboxIntelligence"),
        npgsqlOptions => npgsqlOptions.UseVector());
});

// Mappings
builder.Services.AddAutoMapper(
    cfg => { },
    typeof(MappingProfile));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
