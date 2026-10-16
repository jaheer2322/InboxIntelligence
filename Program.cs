using InboxIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using InboxIntelligence.Mappings;
using InboxIntelligence.Repositories;
using InboxIntelligence.Services;
using InboxIntelligence.Exceptions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

// Register global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();


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

// Use exception handler
app.UseExceptionHandler();

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
