using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IO.Compression;
using System.Text;
using System.Threading.RateLimiting;
using SocialTechsy.SocialNetwork.Application;
using SocialTechsy.SocialNetwork.Infrastructure;
using SocialTechsy.SocialNetwork.Api;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

// Serilog structured logging
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "SocialTechsy")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File("logs/socialtechsy-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    });
builder.Services.AddMemoryCache();
builder.Services.AddExceptionHandler<SocialTechsy.SocialNetwork.Api.ExceptionHandling.ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<SocialTechsy.SocialNetwork.Api.ExceptionHandling.EntityNotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<SocialTechsy.SocialNetwork.Api.ExceptionHandling.UnauthorizedCommandExceptionHandler>();

// Add Application and Infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR-based handlers (needs API layer for hub contexts)
builder.Services.AddScoped<SocialTechsy.SocialNetwork.Application.Interfaces.Services.ILikeNotificationHandler,
    SocialTechsy.SocialNetwork.Api.Services.SignalRLikeNotificationHandler>();
builder.Services.AddScoped<SocialTechsy.SocialNetwork.Application.Interfaces.Services.IChatPushHandler,
    SocialTechsy.SocialNetwork.Api.Services.SignalRChatPushHandler>();

// Health checks
var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver",
        tags: new[] { "db", "sql" });

var mongoConnStr = builder.Configuration["MongoDB:ConnectionString"];
if (!string.IsNullOrEmpty(mongoConnStr))
{
    healthChecksBuilder.AddMongoDb(
        mongodbConnectionString: mongoConnStr,
        name: "mongodb",
        tags: new[] { "db", "mongo" });
}

if (builder.Configuration.GetValue<bool>("Redis:Enabled"))
{
    healthChecksBuilder.AddRedis(
        builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379",
        name: "redis",
        tags: new[] { "cache" });
}

if (builder.Configuration.GetSection("RabbitMQ").GetValue<bool>("Enabled"))
{
    var rabbitConnStr = $"amqp://{builder.Configuration["RabbitMQ:UserName"] ?? "guest"}:{builder.Configuration["RabbitMQ:Password"] ?? "guest"}@{builder.Configuration["RabbitMQ:HostName"] ?? "localhost"}:{builder.Configuration["RabbitMQ:Port"] ?? "5672"}{builder.Configuration["RabbitMQ:VirtualHost"] ?? "/"}";
    healthChecksBuilder.AddRabbitMQ(
        rabbitConnectionString: rabbitConnStr,
        name: "rabbitmq",
        tags: new[] { "messaging" });
}

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Configure SignalR JWT authentication
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",    // Vite dev server
                "http://localhost:3000",    // Next.js default
                "http://localhost:3001",    // Next.js alt
                "http://localhost:3002",
                "http://localhost:3003"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});


// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// Configure response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.SmallestSize);

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
        
        if (builder.Environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();
        }
    });

// Configure SignalR (with optional Redis backplane for multi-instance scaling)
var signalRBuilder = builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 128 * 1024; // 128 KB
    options.StreamBufferCapacity = 20;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumParallelInvocationsPerClient = 2;
});

var redisEnabled = builder.Configuration.GetValue<bool>("Redis:Enabled");
if (redisEnabled)
{
    var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = new StackExchange.Redis.RedisChannel("SocialTechsy", StackExchange.Redis.RedisChannel.PatternMode.Literal);
    });
}

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SocialTechsy.SocialNetwork API",
        Version = "v1",
        Description = "REST API for SocialTechsy.SocialNetwork - A Developer Community Platform"
    });

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SocialTechsy.SocialNetwork API v1");
    });
}

app.UseExceptionHandler(_ => { });
app.UseResponseCompression();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseCors("ReactApp");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Map SignalR hubs with WebSocket transport preference for lower latency
app.MapHub<SocialTechsy.SocialNetwork.Api.Hubs.ChatHub>("/hubs/chat", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets
                       | Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
});
app.MapHub<SocialTechsy.SocialNetwork.Api.Hubs.NotificationHub>("/hubs/notifications");
app.MapHub<SocialTechsy.SocialNetwork.Api.Hubs.QuestionHub>("/hubs/question");
app.MapHub<SocialTechsy.SocialNetwork.Api.Hubs.PresenceHub>("/hubs/presence", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets
                       | Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
});
app.MapHub<SocialTechsy.SocialNetwork.Api.Hubs.ActivityHub>("/hubs/activity");
app.MapHub<SocialTechsy.SocialNetwork.Api.Hubs.CallHub>("/hubs/call");

// Initialize Database
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}

// Sync Redis sequences from MongoDB counters on startup to prevent duplicate IDs
var redisCacheService = app.Services.GetService<SocialTechsy.SocialNetwork.Infrastructure.Redis.RedisChatCacheService>();
var mongoDb = app.Services.GetService<MongoDB.Driver.IMongoDatabase>();
if (redisCacheService != null && mongoDb != null)
{
    await redisCacheService.EnsureSequenceSyncedAsync(mongoDb);
}

app.Run();
