using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Data;
using Coherent.Infrastructure.Middleware;
using Coherent.Infrastructure.Services;
using Coherent.Web.Portal.Hubs;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for ADHICS compliance logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/coherent-web-portal-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

builder.Services.AddSignalR();

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
        new Asp.Versioning.QueryStringApiVersionReader("api-version"),
        new Asp.Versioning.HeaderApiVersionReader("X-Api-Version"),
        new Asp.Versioning.UrlSegmentApiVersionReader()
    );
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddHttpClient("MobileBackend");

builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with API versioning
builder.Services.AddSwaggerGen(options =>
{
    // Version 1 - Web Portal (JWT Authentication)
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Coherent Web Portal API - Version 1",
        Version = "v1",
        Description = "ADHICS-compliant Web Portal API with JWT authentication and RBAC (For Web Portal)"
    });

    // Version 2 - Mobile App (Security Key Authentication)
    options.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Coherent Mobile App API - Version 2",
        Version = "v2",
        Description = "Mobile App Backend API with Security Key authentication (For Mobile App)"
    });

    // JWT Bearer Authentication (Version 1)
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using Bearer scheme. Used for Version 1 (Web Portal)"
    });

    // Security Key Authentication (Version 2)
    options.AddSecurityDefinition("SecurityKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-Security-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Security Key for third-party/mobile app authentication. Used for Version 2 (Mobile App)"
    });

    // Apply security requirements based on version
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "SecurityKey"
                }
            },
            Array.Empty<string>()
        }
    });

    // Group by version - simplified approach
    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        var actionDescriptor = apiDesc.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        if (actionDescriptor == null) return false;

        var apiVersionAttribute = actionDescriptor.ControllerTypeInfo
            .GetCustomAttributes(true)
            .OfType<Asp.Versioning.ApiVersionAttribute>()
            .FirstOrDefault();

        if (apiVersionAttribute == null) return false;

        var version = apiVersionAttribute.Versions.FirstOrDefault();
        if (version == null) return false;

        // Match v1 to Version 1.0 and v2 to Version 2.0
        var versionString = $"v{version.MajorVersion}";
        return versionString == docName;
    });
});

// JWT Authentication Configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true; // ADHICS: Enforce HTTPS
    options.SaveToken = true;
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var path = context.HttpContext.Request.Path;

            // For SignalR hub connections
            if (path.StartsWithSegments("/hubs/crm-chat"))
            {
                // First check query string token (web clients)
                var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    context.Token = accessToken;
                    return Task.CompletedTask;
                }

                // Check for API key (mobile backend service connection)
                var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault()
                    ?? context.Request.Query["api_key"].FirstOrDefault();
                
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    var expectedKey = context.HttpContext.RequestServices
                        .GetRequiredService<IConfiguration>()["SignalR:ServiceApiKey"];
                    
                    if (!string.IsNullOrWhiteSpace(expectedKey) && apiKey == expectedKey)
                    {
                        // Mark as service connection and skip JWT validation
                        context.HttpContext.Items["IsServiceConnection"] = true;
                        context.NoResult();
                        return Task.CompletedTask;
                    }
                }
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var rawToken = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrWhiteSpace(rawToken))
                return;

            try
            {
                var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes)
                    sb.Append(b.ToString("x2"));
                var tokenHash = sb.ToString();

                var factory = context.HttpContext.RequestServices.GetRequiredService<DatabaseConnectionFactory>();
                using var connection = factory.CreatePrimaryConnection();

                var isRevoked = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.SecAuthSession WHERE TokenHash = @TokenHash AND IsLoggedOut = 1) THEN 1 ELSE 0 END",
                    new { TokenHash = tokenHash }) == 1;

                if (isRevoked)
                    context.Fail("Token has been logged out");
            }
            catch
            {
                // Ignore revocation check failures and fall back to standard JWT validation.
            }
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS Configuration (ADHICS compliance - restrict origins)
builder.Services.AddCors(options =>
{
    options.AddPolicy("ADHICSPolicy", policy =>
    {
        // Allow any origin for local network development
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Rate Limiting (ADHICS compliance)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken);
    };
});

// Register application services
builder.Services.AddSingleton<DatabaseConnectionFactory>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IThirdPartyService, ThirdPartyService>();

// Register Chat Repositories
builder.Services.AddScoped<IChatRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreateSecondaryConnection();
    return new Coherent.Infrastructure.Repositories.ChatRepository(connection);
});

builder.Services.AddScoped<IChatWebhookOutboxRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreatePrimaryConnection();
    return new Coherent.Infrastructure.Repositories.ChatWebhookOutboxRepository(connection);
});

builder.Services.AddHostedService<ChatWebhookBackgroundService>();

// Register Security Repository (uses primary database - UEMedical_For_R&D)
builder.Services.AddScoped<ISecurityRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreatePrimaryConnection();
    return new Coherent.Infrastructure.Repositories.SecurityRepository(connection);
});

// Register Patient Repository
builder.Services.AddScoped<IPatientRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreatePrimaryConnection();
    return new Coherent.Infrastructure.Repositories.PatientRepository(connection);
});

// Register Appointment Repository (uses both databases)
builder.Services.AddScoped<IAppointmentRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var primaryConnection = factory.CreatePrimaryConnection();
    var secondaryConnection = factory.CreateSecondaryConnection();
    return new Coherent.Infrastructure.Repositories.AppointmentRepository(primaryConnection, secondaryConnection);
});

// Register Doctor Repository (uses secondary database - CoherentMobApp)
builder.Services.AddScoped<IDoctorRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreateSecondaryConnection();
    return new Coherent.Infrastructure.Repositories.DoctorRepository(connection);
});

// Register CRM Master Data Repositories (uses secondary database - CoherentMobApp)
builder.Services.AddScoped<ICrmDoctorRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreateSecondaryConnection();
    return new Coherent.Infrastructure.Repositories.CrmDoctorRepository(connection);
});

builder.Services.AddScoped<ICrmFacilityRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreateSecondaryConnection();
    return new Coherent.Infrastructure.Repositories.CrmFacilityRepository(connection);
});

 builder.Services.AddScoped<ICrmSpecialityRepository>(provider =>
 {
     var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
     var connection = factory.CreateSecondaryConnection();
     return new Coherent.Infrastructure.Repositories.CrmSpecialityRepository(connection);
 });

builder.Services.AddScoped<ICrmDoctorFacilityRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreateSecondaryConnection();
    return new Coherent.Infrastructure.Repositories.CrmDoctorFacilityRepository(connection);
});

builder.Services.AddScoped<IFacilityServiceRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreateSecondaryConnection();
    return new Coherent.Infrastructure.Repositories.FacilityServiceRepository(connection);
});

builder.Services.AddScoped<ISubServiceRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreateSecondaryConnection();
    return new Coherent.Infrastructure.Repositories.SubServiceRepository(connection);
});

// Register Patient Health Repository (uses primary database - UEMedical_For_R&D)
builder.Services.AddScoped<IPatientHealthRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseConnectionFactory>();
    var connection = factory.CreatePrimaryConnection();
    return new Coherent.Infrastructure.Repositories.PatientHealthRepository(connection);
});

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "V1 - Web Portal (JWT Auth)");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "V2 - Mobile App (Security Key Auth)");
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
    });
//}

// ADHICS Compliance: Enforce HTTPS
app.UseHttpsRedirection();

app.UseStaticFiles();

// Security Headers (ADHICS compliance)
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});

// CORS
app.UseCors("ADHICSPolicy");

// Rate Limiting
app.UseRateLimiter();

// Serilog request logging
app.UseSerilogRequestLogging();

// Custom middleware
app.UseMiddleware<ThirdPartyAuthMiddleware>();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapHealthChecks("/health");

// Controllers
app.MapControllers();

app.MapHub<CrmChatHub>("/hubs/crm-chat");

try
{
    Log.Information("Starting Coherent Web Portal API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
