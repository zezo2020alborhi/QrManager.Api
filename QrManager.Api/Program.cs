//using System.Diagnostics;
//using System.Text.Json.Serialization;
//using Microsoft.AspNetCore.DataProtection;
//using Microsoft.AspNetCore.Diagnostics;
//using Microsoft.EntityFrameworkCore;
//using QrManager.Api.Data;
//using QrManager.Api.Endpoints;
//using QrManager.Api.Observability;
//using QrManager.Api.Options;
//using QrManager.Api.Security;
//using QrManager.Api.Services;

//var builder = WebApplication.CreateBuilder(args);

//// ============================================================
//// Logging
//// ============================================================

//builder.Logging.AddProvider(
//    new DailyFileLoggerProvider(
//        Path.Combine(builder.Environment.ContentRootPath, "logs")
//    )
//);

//// ============================================================
//// Configuration
//// ============================================================

//builder.Services.Configure<SecurityOptions>(
//    builder.Configuration.GetSection("Security")
//);

//builder.Services.Configure<GoogleDriveOptions>(
//    builder.Configuration.GetSection("GoogleDrive")
//);

//builder.Services.Configure<DatabaseOptions>(
//    builder.Configuration.GetSection("Database")
//);

//// ============================================================
//// Database
//// ============================================================

//builder.Services.AddDbContext<QrManagerDbContext>(options =>
//{
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("SqlServer")
//    );
//});

//// ============================================================
//// Data Protection
//// ============================================================

//var dataProtectionPath =
//    builder.Configuration["DataProtection:KeysPath"];

//if (string.IsNullOrWhiteSpace(dataProtectionPath))
//{
//    dataProtectionPath = Path.Combine(
//        builder.Environment.ContentRootPath,
//        "data-protection-keys"
//    );
//}

//Directory.CreateDirectory(dataProtectionPath);

//builder.Services
//    .AddDataProtection()
//    .PersistKeysToFileSystem(
//        new DirectoryInfo(dataProtectionPath)
//    );

//// ============================================================
//// HTTP Client
//// ============================================================

//builder.Services.AddHttpClient();

//// ============================================================
//// Services
//// ============================================================

//builder.Services.AddScoped<AdminApiKeyEndpointFilter>();
//builder.Services.AddScoped<GoogleDriveOAuthService>();

//// ============================================================
//// Swagger
//// ============================================================

//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//// ============================================================
//// JSON
//// ============================================================

//builder.Services.ConfigureHttpJsonOptions(options =>
//{
//    options.SerializerOptions.Converters.Add(
//        new JsonStringEnumConverter()
//    );
//});

//// ============================================================
//// CORS
//// ============================================================
////
//// Accept requests from ANY origin.
//// This is useful for Flutter Web and browser clients.
////
//// NOTE:
//// Native Flutter Android/iOS apps are not restricted by browser CORS.
////
//// Do NOT use AllowAnyOrigin() together with AllowCredentials().
//// We are intentionally NOT enabling credentials here.
//// ============================================================

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowFlutter", policy =>
//    {
//        policy
//            .AllowAnyOrigin()
//            .AllowAnyHeader()
//            .AllowAnyMethod();
//    });
//});

//// ============================================================
//// Kestrel
//// ============================================================
////
//// Listen on ALL network interfaces.
////
//// Flutter can connect using:
////
//// http://localhost:57510
//// http://127.0.0.1:57510
//// http://192.168.x.x:57510
//// http://10.x.x.x:57510
//// http://<server-ip>:57510
////
//// The application is no longer limited to Development mode.
//// ============================================================

//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.ListenAnyIP(57510);
//});

//// ============================================================
//// Build
//// ============================================================

//var app = builder.Build();

//var logger = app.Logger;

//// ============================================================
//// Admin API Key Configuration
//// ============================================================

//var configuredAdminKey =
//    app.Configuration["Security:AdminApiKey"];

//var adminKeyConfigured =
//    !string.IsNullOrWhiteSpace(configuredAdminKey) &&
//    !configuredAdminKey.StartsWith(
//        "SET_",
//        StringComparison.OrdinalIgnoreCase
//    );

//if (!adminKeyConfigured)
//{
//    logger.LogWarning(
//        "Security__AdminApiKey is not configured. " +
//        "Every protected /api route will return 401 until it is supplied."
//    );
//}
//else
//{
//    logger.LogInformation(
//        "Admin API key configuration is present. " +
//        "The key value is intentionally not logged."
//    );
//}

//// ============================================================
//// Swagger
//// ============================================================

//app.UseSwagger();
//app.UseSwaggerUI();

//// ============================================================
//// Global Exception Handler
//// ============================================================

//app.UseExceptionHandler(errorApp =>
//{
//    errorApp.Run(async context =>
//    {
//        var exception =
//            context.Features
//                .Get<IExceptionHandlerFeature>()?
//                .Error;

//        logger.LogError(
//            exception,
//            "Unhandled server error. TraceId {TraceId}",
//            context.TraceIdentifier
//        );

//        context.Response.StatusCode =
//            StatusCodes.Status500InternalServerError;

//        await context.Response.WriteAsJsonAsync(
//            new
//            {
//                error = "حدث خطأ غير متوقع في الخادم.",
//                traceId = context.TraceIdentifier
//            }
//        );
//    });
//});

//// ============================================================
//// HTTPS
//// ============================================================
////
//// We intentionally DO NOT call UseHttpsRedirection() here.
////
//// This allows Flutter to connect using:
////
//// http://SERVER-IP:57510
////
//// If you need HTTPS later, configure HTTPS separately.
//// ============================================================

//// app.UseHttpsRedirection();

//// ============================================================
//// CORS
//// ============================================================

//app.UseCors("AllowFlutter");

//// ============================================================
//// HTTP Request Logging
//// ============================================================

//app.Use(async (context, next) =>
//{
//    var stopwatch = Stopwatch.StartNew();

//    try
//    {
//        await next();
//    }
//    finally
//    {
//        logger.LogInformation(
//            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms. TraceId {TraceId}",
//            context.Request.Method,
//            context.Request.Path,
//            context.Response.StatusCode,
//            stopwatch.ElapsedMilliseconds,
//            context.TraceIdentifier
//        );
//    }
//});

//// ============================================================
//// Database Migration
//// ============================================================

//using (var scope = app.Services.CreateScope())
//{
//    var databaseOptions =
//        scope.ServiceProvider
//            .GetRequiredService<
//                Microsoft.Extensions.Options.IOptions<DatabaseOptions>
//            >()
//            .Value;

//    if (databaseOptions.AutoCreateOnStartup)
//    {
//        await scope.ServiceProvider
//            .GetRequiredService<QrManagerDbContext>()
//            .Database
//            .MigrateAsync();
//    }
//}

//// ============================================================
//// Endpoints
//// ============================================================

//app.MapQrManagerEndpoints();
//app.MapGoogleDriveEndpoints();

//// ============================================================
//// Start
//// ============================================================

//logger.LogInformation(
//    "QrManager API is starting on http://0.0.0.0:57510"
//);

//app.Run();
using System.Diagnostics;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

using QrManager.Api.Data;
using QrManager.Api.Endpoints;
using QrManager.Api.Observability;
using QrManager.Api.Options;
using QrManager.Api.Security;
using QrManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Logging
// ============================================================

builder.Logging.AddProvider(
    new DailyFileLoggerProvider(
        Path.Combine(
            builder.Environment.ContentRootPath,
            "logs"
        )
    )
);

// ============================================================
// Configuration
// ============================================================

builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection("Security")
);

builder.Services.Configure<GoogleDriveOptions>(
    builder.Configuration.GetSection("GoogleDrive")
);

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection("Database")
);

// ============================================================
// Database
// ============================================================

builder.Services.AddDbContext<QrManagerDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServer")
    );
});

// ============================================================
// Data Protection
// ============================================================

var dataProtectionPath =
    builder.Configuration["DataProtection:KeysPath"];

if (string.IsNullOrWhiteSpace(dataProtectionPath))
{
    dataProtectionPath = Path.Combine(
        builder.Environment.ContentRootPath,
        "data-protection-keys"
    );
}

// Ensure the directory exists
Directory.CreateDirectory(dataProtectionPath);

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(dataProtectionPath)
    );

// ============================================================
// HTTP Client
// ============================================================

builder.Services.AddHttpClient();

// ============================================================
// Services
// ============================================================

builder.Services.AddScoped<AdminApiKeyEndpointFilter>();
builder.Services.AddScoped<GoogleDriveOAuthService>();

// ============================================================
// Swagger
// ============================================================
//
// Swagger is intentionally enabled in ALL environments,
// including Production.
//
// Swagger UI:
//     /swagger
//
// Swagger JSON:
//     /swagger/v1/swagger.json
//
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

// ============================================================
// JSON
// ============================================================

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter()
    );
});

// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutter", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ============================================================
// IMPORTANT:
//
// DO NOT configure Kestrel.ListenAnyIP() here.
//
// IIS / ASP.NET Core Module will control the application
// port when deployed to IIS / FreeASPHosting.
//
// Local development can still use launchSettings.json.
//
// ============================================================

// ============================================================
// Build
// ============================================================

var app = builder.Build();

var logger = app.Logger;

// ============================================================
// Admin API Key Configuration
// ============================================================

var configuredAdminKey =
    app.Configuration["Security:AdminApiKey"];

var adminKeyConfigured =
    !string.IsNullOrWhiteSpace(configuredAdminKey) &&
    !configuredAdminKey.StartsWith(
        "SET_",
        StringComparison.OrdinalIgnoreCase
    );

if (!adminKeyConfigured)
{
    logger.LogWarning(
        "Security__AdminApiKey is not configured. " +
        "Every protected /api route will return 401 until it is supplied."
    );
}
else
{
    logger.LogInformation(
        "Admin API key configuration is present. " +
        "The key value is intentionally not logged."
    );
}

// ============================================================
// Global Exception Handler
// ============================================================
//
// Put the exception handler early in the pipeline so that
// application exceptions are handled consistently.
//
// ============================================================

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception =
            context.Features
                .Get<IExceptionHandlerFeature>()?
                .Error;

        logger.LogError(
            exception,
            "Unhandled server error. TraceId {TraceId}",
            context.TraceIdentifier
        );

        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(
            new
            {
                error = "حدث خطأ غير متوقع في الخادم.",
                traceId = context.TraceIdentifier
            }
        );
    });
});

// ============================================================
// Swagger
// ============================================================
//
// IMPORTANT:
// No Environment.IsDevelopment() condition here.
//
// Swagger is available in Production.
//
// ============================================================

app.UseSwagger(options =>
{
    options.RouteTemplate =
        "swagger/{documentName}/swagger.json";
});

app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";

    // Relative URL is more IIS/virtual-directory friendly
    options.SwaggerEndpoint(
        "./v1/swagger.json",
        "QrManager API v1"
    );

    options.DocumentTitle = "QrManager API";

    options.DisplayRequestDuration();

    options.EnableDeepLinking();

    options.EnableFilter();
});

// ============================================================
// HTTPS
// ============================================================
//
// We intentionally do NOT call:
//
//     app.UseHttpsRedirection();
//
// The hosting provider / IIS can terminate HTTPS.
//
// This also prevents redirects from:
//
//     https://domain
//
// to an internal HTTP port.
//
// ============================================================

// app.UseHttpsRedirection();

// ============================================================
// CORS
// ============================================================

app.UseCors("AllowFlutter");

// ============================================================
// HTTP Request Logging
// ============================================================

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        await next();
    }
    finally
    {
        logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms. TraceId {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            context.TraceIdentifier
        );
    }
});

// ============================================================
// Health / Root Endpoint
// ============================================================
//
// Useful for verifying that IIS is actually reaching
// the ASP.NET Core application.
//
// Test:
//
//     https://taiztwo.bsite.net/
//
// ============================================================

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        application = "QrManager API",
        status = "Running",
        environment = app.Environment.EnvironmentName,
        swagger = "/swagger"
    });
});

// ============================================================
// Database Migration
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var databaseOptions =
        scope.ServiceProvider
            .GetRequiredService<
                Microsoft.Extensions.Options.IOptions<DatabaseOptions>
            >()
            .Value;

    if (databaseOptions.AutoCreateOnStartup)
    {
        logger.LogInformation(
            "Database AutoCreateOnStartup is enabled. " +
            "Running EF Core database migrations..."
        );

        await scope.ServiceProvider
            .GetRequiredService<QrManagerDbContext>()
            .Database
            .MigrateAsync();

        logger.LogInformation(
            "Database migration completed successfully."
        );
    }
    else
    {
        logger.LogInformation(
            "Database AutoCreateOnStartup is disabled."
        );
    }
}

// ============================================================
// Endpoints
// ============================================================

app.MapQrManagerEndpoints();

app.MapGoogleDriveEndpoints();

// ============================================================
// Startup
// ============================================================

logger.LogInformation(
    "QrManager API started successfully."
);

logger.LogInformation(
    "Environment: {Environment}",
    app.Environment.EnvironmentName
);

 

app.Run();