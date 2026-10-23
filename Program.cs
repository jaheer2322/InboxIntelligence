using InboxIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using InboxIntelligence.Mappings;
using InboxIntelligence.Repositories;
using InboxIntelligence.Services;
using InboxIntelligence.Exceptions;
using Serilog.Events;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override(
        "Microsoft.EntityFrameworkCore.Database.Command",
        LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/inbox-intelligence-.log",
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        retainedFileCountLimit: 14,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

// Register Gemini services
builder.Services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>();
builder.Services.AddHttpClient<IGeminiAnswerService, GeminiAnswerService>();

// Register global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Register BackgroundService (Embed queue)
builder.Services.AddHostedService<EmbeddingBackgroundService>();

// Register dbcontext
builder.Services.AddDbContext<InboxIntelligenceDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("InboxIntelligence"),
        npgsqlOptions => npgsqlOptions.UseVector());
});

// Register services and repositories
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
