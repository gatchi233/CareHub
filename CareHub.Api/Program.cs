using System.Net.Http.Headers;
using System.Text;
using CareHub.Api.Data;
using CareHub.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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

// DB
builder.Services.AddDbContext<CareHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CareHubDb")));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.AddScoped<JwtTokenService>();

var auth = builder.Configuration.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.SigningKey));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = auth.Issuer,
            ValidAudience = auth.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// CORS (Dev only policy name kept stable for later)
const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});

builder.Services.AddAuthorization();

// AI rate limiter — 85% of Groq free tier: 30 RPM / 1K RPD
builder.Services.AddSingleton(new AiRateLimiter(
    globalRpm: 25,   // Groq free: 30 RPM  → 85% = 25
    globalRpd: 850,  // Groq free: 1K RPD  → 85% = 850
    perUserRpm: 8,   // Single user can't hog more than ~1/3 of RPM
    perUserRpd: 170  // Single user daily cap
));

// Groq AI service
var groqKey = builder.Configuration["Groq:ApiKey"] ?? "";
if (!string.IsNullOrWhiteSpace(groqKey))
{
    builder.Services.AddHttpClient<GroqAiService>(client =>
    {
        client.BaseAddress = new Uri("https://api.groq.com/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", groqKey);
        client.Timeout = TimeSpan.FromSeconds(30);
    });
}
else
{
    // Register a dummy so DI doesn't fail — controller returns 503
    builder.Services.AddHttpClient<GroqAiService>(client =>
    {
        client.BaseAddress = new Uri("https://api.groq.com/");
        client.Timeout = TimeSpan.FromSeconds(5);
    });
}

var app = builder.Build();

// Ensure database is created/migrated and seed once from shared JSON source.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CareHubDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "AppUsers" (
            "Id" uuid NOT NULL,
            "Username" text NOT NULL,
            "PasswordHash" text NOT NULL,
            "DisplayName" text NOT NULL,
            "Role" text NOT NULL,
            "ResidentId" uuid NULL,
            CONSTRAINT "PK_AppUsers" PRIMARY KEY ("Id")
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_AppUsers_Username" ON "AppUsers" ("Username");
    """);
}
await DataSeedService.SeedFromSharedJsonAsync(app.Services, app.Configuration, app.Environment);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(DevCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.MapControllers();

app.Run();
