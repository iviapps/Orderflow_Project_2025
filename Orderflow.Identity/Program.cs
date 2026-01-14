using Asp.Versioning;
using FluentValidation;
using MassTransit;
using Scalar.AspNetCore;
using Orderflow.Identity.Extensions;
using Orderflow.Identity.Services;
using Orderflow.Identity.Services.Auth;
using Orderflow.Identity.Services.Roles;
using Orderflow.Identity.Services.Users;
using Orderflow.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// ASPIRE SERVICE DEFAULTS
// ============================================
builder.AddServiceDefaults();

// ============================================
// OPENAPI / SCALAR
// ============================================
builder.Services.AddOpenApi("v1", options =>
{
    options.ConfigureDocumentInfo(
        "Orderflow Identity API V1",
        "v1",
        "Authentication API using Controllers with JWT Bearer authentication");
    options.AddJwtBearerSecurity();
    options.FilterByApiVersion("v1");
});

// ============================================
// CONTROLLERS + AUTHORIZATION
// ============================================
builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ============================================
// API VERSIONING
// ============================================
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// ============================================
// FLUENT VALIDATION
// ============================================
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ============================================
// CORS
// ============================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// ============================================
// IDENTITY + DATABASE + GOOGLE OAUTH
// ============================================
var connectionString = builder.Configuration.GetConnectionString("identitydb")!;
builder.Services.AddIdentityWithGoogle(builder.Configuration, connectionString);

// ============================================
// JWT AUTHENTICATION
// ============================================
builder.Services.AddJwtAuthentication(builder.Configuration);

// ============================================
// MASSTRANSIT + RABBITMQ
// ============================================
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var rabbitConnectionString = configuration.GetConnectionString("messaging");

        if (!string.IsNullOrEmpty(rabbitConnectionString))
        {
            cfg.Host(new Uri(rabbitConnectionString));
        }

        cfg.ConfigureEndpoints(context);
    });
});

// ============================================
// SERVICES
// ============================================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();  

var app = builder.Build();

// ============================================
// DEVELOPMENT PIPELINE
// ============================================
if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDevelopmentDataAsync();

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Orderflow Identity API")
            .AddDocument("v1", "V1 - Controllers", "/openapi/v1.json", isDefault: true);
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Orderflow Identity API V1");
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapControllers();

await app.RunAsync();