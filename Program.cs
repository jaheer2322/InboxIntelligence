using InboxIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using InboxIntelligence.Mappings;
using InboxIntelligence.Repositories;
using InboxIntelligence.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register dbcontext
builder.Services.AddDbContext<InboxIntelligenceDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("InboxIntelligence"),
        npgsqlOptions => npgsqlOptions.UseVector());
});

// Register Service, Repositories
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();

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
