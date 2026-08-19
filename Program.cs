using MIC.risk.Authorization;
using MIC.risk.Data;
using MIC.risk.Extensions;
using MIC.risk.Interfaces;
using MIC.risk.Middleware;
using MIC.risk.Models;
using MIC.risk.Options;
using MIC.risk.Service;
using MIC.risk.Services;
using MIC.risk.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// JWT__SigningKey as an environment variable already binds to JWT:SigningKey through the
// default configuration sources, so a single section covers both.
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.SigningKey),
        "JWT signing key is not configured. Set JWT:SigningKey or environment variable JWT__SigningKey.")
    .Validate(
        options => options.AccessTokenMinutes > 0,
        "JWT:AccessTokenMinutes must be greater than zero.")
    .Validate(
        options => options.RefreshTokenDays > 0,
        "JWT:RefreshTokenDays must be greater than zero.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException(
        "JWT signing key is not configured. Set JWT:SigningKey or environment variable JWT__SigningKey.");
}

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

// Makes framework-generated failures (415, 406, unhandled 500s) match the ProblemDetails
// shape the controllers and the exception middleware already produce.
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Paste only the JWT token. Example: eyJhbGciOi..."
        };

        return Task.CompletedTask;
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length > 0)
        {
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("Content-Disposition")

                // Required for the refresh-token cookie to be sent and set cross-origin.
                // Incompatible with AllowAnyOrigin by specification, hence the explicit list.
                .AllowCredentials();
        }
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDBContext>();

builder.Services.Configure<FileUploadOptions>(
    builder.Configuration.GetSection(FileUploadOptions.SectionName));

builder.Services.Configure<FormOptions>(options =>
{
    var maxUploadBytes = builder.Configuration
        .GetSection(FileUploadOptions.SectionName)
        .GetValue<long?>(nameof(FileUploadOptions.MaxFileSizeBytes))
        ?? 10 * 1024 * 1024;

    options.MultipartBodyLengthLimit = maxUploadBytes;
});

builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDBContext>()

    // Required by the admin password reset, which mints and immediately redeems a reset token
    // so that Identity's own password policy and security stamp are applied.
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),

        // Access tokens are short-lived, so the default five-minute grace period would
        // materially extend them. Keep a small allowance for clock drift instead.
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                context.Fail("The token does not identify an employee account.");
                return;
            }

            var dbContext = context.HttpContext.RequestServices
                .GetRequiredService<ApplicationDBContext>();

            // Deliberately per request: this is what makes deactivating an employee take
            // effect immediately rather than whenever their access token happens to expire.
            var isActiveEmployee = await dbContext.Employees
                .AsNoTracking()
                .AnyAsync(
                    employee => employee.IdentityUserId == userId && employee.Active,
                    context.HttpContext.RequestAborted);

            if (!isActiveEmployee)
            {
                context.Fail("The employee account is inactive.");
            }
        },

        // Without these, a rejected or forbidden request returns an empty body, which the
        // OpenAPI document says is ProblemDetails. Now it actually is.
        OnChallenge = context =>
        {
            context.HandleResponse();

            return WriteProblemAsync(
                context.HttpContext,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource.");
        },

        OnForbidden = context => WriteProblemAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Forbidden",
            "You do not have permission to access this resource.")
    };
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IRiskReportService, RiskReportService>();
builder.Services.AddScoped<IAuthorizationHandler, RiskReportOwnerHandler>();
builder.Services.AddScoped<IRiskService, RiskService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IResourceEngagementService, ResourceEngagementService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IRiskActionService, RiskActionService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EditOrViewRiskReport", policy =>
        policy.Requirements.Add(new SameOwnerRequirement()));
});

var app = builder.Build();

var fileUploadOptions = app.Services.GetRequiredService<IOptions<FileUploadOptions>>().Value;
var uploadsRoot = UploadPath.Resolve(fileUploadOptions, app.Environment.ContentRootPath);
Directory.CreateDirectory(uploadsRoot);

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddPreferredSecuritySchemes("Bearer");
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// Uploaded files live outside the content root, so they need their own provider. The request
// path is unchanged, which keeps every URL already stored against a resource working.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = $"/{fileUploadOptions.UploadSubdirectory.Trim('/')}"
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail) =>
    ProblemResponseWriter.WriteAsync(context, statusCode, title, detail);
