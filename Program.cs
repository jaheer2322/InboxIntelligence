using InboxIntelligence.Data;
using Microsoft.EntityFrameworkCore;
using InboxIntelligence.Mappings;
using InboxIntelligence.Repositories;
using InboxIntelligence.Services;
using InboxIntelligence.Exceptions;
using Serilog.Events;
using Serilog;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

var debugLogs = builder.Configuration.GetValue<bool>("DebugLogs");

// Keep warnings/errors visible; enable debug diagnostics only when requested.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(debugLogs ? LogEventLevel.Debug : LogEventLevel.Warning)
    .MinimumLevel.Override(
        "Microsoft",
        debugLogs ? LogEventLevel.Warning : LogEventLevel.Error)
    .MinimumLevel.Override(
        "Microsoft.EntityFrameworkCore",
        debugLogs ? LogEventLevel.Warning : LogEventLevel.Error)
    .MinimumLevel.Override(
        "Microsoft.EntityFrameworkCore.Database.Command",
        debugLogs ? LogEventLevel.Warning : LogEventLevel.Error)
    .MinimumLevel.Override(
        "Microsoft.EntityFrameworkCore.Infrastructure",
        debugLogs ? LogEventLevel.Warning : LogEventLevel.Error)
    .MinimumLevel.Override(
        "Microsoft.EntityFrameworkCore.Database.Connection",
        debugLogs ? LogEventLevel.Warning : LogEventLevel.Error)
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
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
    })
    .AddMvc();

builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

// Register GeminiEmbeddingService for IEmbeddingService
builder.Services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>();

// Register GeminiAnswerService for IGeminiAnswerService
builder.Services.AddHttpClient<IGeminiAnswerService, GeminiAnswerService>();

// Register GeminiAgentClient for IGeminiAgentClient
builder.Services.AddHttpClient<IGeminiAgentClient, GeminiAgentClient>();

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

// Register Service, Repositories
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Register ToolExecutor
builder.Services.AddScoped<EmailToolExecutor>();

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
