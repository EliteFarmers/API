using System.Net.Http.Headers;
using System.Security.Claims;
using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Auth.Models;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Npgsql;

namespace EliteAPI.Tests.Guides;

/// <summary>
/// App fixture with PostgreSQL testcontainer for Guide system integration tests.
/// </summary>
public class GuideTestApp : AppFixture<Program>
{
    private PostgreSqlContainer _postgres = null!;
    private string _jwtSigningKey = null!;
    
    // Test user IDs
    public const ulong RegularUserId = 100001;
    public const ulong ModeratorUserId = 100002;
    public const ulong AdminUserId = 100003;
    public const ulong RestrictedUserId = 100004;
    
    // Pre-configured clients with JWT tokens
    public HttpClient AnonymousClient { get; private set; } = null!;
    public HttpClient RegularUserClient { get; private set; } = null!;
    public HttpClient ModeratorClient { get; private set; } = null!;
    public HttpClient AdminClient { get; private set; } = null!;
    public HttpClient RestrictedUserClient { get; private set; } = null!;

    protected override async ValueTask PreSetupAsync()
    {
        // Set required environment variables for services that require them
        Environment.SetEnvironmentVariable("DISCORD_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("DISCORD_CLIENT_SECRET", "test-client-secret");
        Environment.SetEnvironmentVariable("DISCORD_BOT_TOKEN", "test-bot-token");
        Environment.SetEnvironmentVariable("HYPIXEL_API_KEY", "test-api-key");
        
        // Start PostgreSQL container
        _postgres = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("eliteapi_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
        
        await _postgres.StartAsync();
    }

    public async Task CleanUpGuidesAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var guides = await db.Guides.Where(g => g.AuthorId == RegularUserId).ToListAsync();
        db.Guides.RemoveRange(guides);
        await db.SaveChangesAsync();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await SetupAsync();
    }

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // Remove existing DbContext registration
        var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DataContext>));
        if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);
        
        var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(System.Data.Common.DbConnection));
        if (dbConnectionDescriptor != null) services.Remove(dbConnectionDescriptor);
        
        // Remove the context itself to ensure AddDbContext registers the new one correctly
        var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DataContext));
        if (contextDescriptor != null) services.Remove(contextDescriptor);
        
        // Add test database with Dynamic JSON enabled (required for JSONB columns)
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_postgres.GetConnectionString());
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();
        
        services.AddDbContext<DataContext>(options =>
        {
            options.UseNpgsql(dataSource);
        });

        // Force default schemes to Bearer
        services.Configure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        });

        // Configure validation parameters and add logging
        services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters.ValidIssuer = "eliteapi";
            options.TokenValidationParameters.ValidAudience = "eliteapi";
            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidateLifetime = true;
            options.TokenValidationParameters.IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSigningKey));
        });
    }

    protected override async ValueTask SetupAsync()
    {
        // Get JWT signing key from configuration
        var configuration = Services.GetRequiredService<IConfiguration>();
        _jwtSigningKey = configuration["Jwt:Secret"] 
            ?? throw new InvalidOperationException("Jwt:Secret not found in configuration");
        
        // Create and migrate database
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        await db.Database.EnsureCreatedAsync();
        
        // Seed test users
        await SeedTestUsers(db);
        
        // Create pre-configured clients with JWT tokens
        AnonymousClient = CreateClient();
        
        RegularUserClient = CreateClient(c =>
        {
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", GenerateJwtToken(RegularUserId.ToString(), "testuser", ApiUserPolicies.User));
        });
        
        ModeratorClient = CreateClient(c =>
        {
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", GenerateJwtToken(ModeratorUserId.ToString(), "testmod", 
                    ApiUserPolicies.User, ApiUserPolicies.Moderator));
        });
        
        AdminClient = CreateClient(c =>
        {
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", GenerateJwtToken(AdminUserId.ToString(), "testadmin", 
                    ApiUserPolicies.User, ApiUserPolicies.Moderator, ApiUserPolicies.Admin));
        });
        
        RestrictedUserClient = CreateClient(c =>
        {
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", GenerateJwtToken(RestrictedUserId.ToString(), "restricted", ApiUserPolicies.User));
        });
    }

    /// <summary>
    /// Generate a JWT token with the same signing key used by the app.
    /// </summary>
    private string GenerateJwtToken(string userId, string username, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimNames.Name, username),
            new(ClaimNames.NameId, userId),
            new(ClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimNames.Avatar, ""),
            new(ClaimNames.Ign, "TestPlayer"),
            new(ClaimNames.FormattedIgn, "TestPlayer"),
            new(ClaimNames.Uuid, ""),
            new(ClaimNames.Flags, ""),
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimNames.Role, role));
        }

        var token = JwtBearer.CreateToken(o =>
        {
            o.SigningKey = _jwtSigningKey;
            o.Issuer = "eliteapi";
            o.Audience = "eliteapi";
            o.User.Claims.AddRange(claims);
            o.ExpireAt = DateTime.UtcNow.AddHours(1);
        });

        return token;
    }

    private async Task SeedTestUsers(DataContext db)
    {
        // Skip if already seeded
        if (await db.Accounts.AnyAsync(a => a.Id == RegularUserId))
            return;
        
        // Create ApiUser records (for UserManager) and EliteAccount records
        var users = new[]
        {
            (RegularUserId, "TestUser", "testuser", PermissionFlags.None),
            (ModeratorUserId, "TestMod", "testmod", PermissionFlags.Moderator),
            (AdminUserId, "TestAdmin", "testadmin", PermissionFlags.Admin),
            (RestrictedUserId, "RestrictedUser", "restricted", PermissionFlags.RestrictedFromGuides | PermissionFlags.RestrictedFromComments)
        };

        foreach (var (id, displayName, username, permissions) in users)
        {
            // Create EliteAccount
            var account = new EliteAccount
            {
                Id = id,
                DisplayName = displayName,
                Username = username,
                Permissions = permissions
            };
            db.Accounts.Add(account);

            // Create ApiUser linked to account
            var apiUser = new ApiUser
            {
                Id = id.ToString(),
                UserName = username,
                NormalizedUserName = username.ToUpperInvariant(),
                Email = $"{username}@test.local",
                NormalizedEmail = $"{username.ToUpperInvariant()}@TEST.LOCAL",
                AccountId = id
            };
            db.Users.Add(apiUser);
        }

        await db.SaveChangesAsync();
    }

    protected override async ValueTask TearDownAsync()
    {
        AnonymousClient?.Dispose();
        RegularUserClient?.Dispose();
        ModeratorClient?.Dispose();
        AdminClient?.Dispose();
        RestrictedUserClient?.Dispose();
        
        if (_postgres != null)
        {
            await _postgres.DisposeAsync();
        }
    }
}
