using System.Net.Http.Headers;
using System.Security.Claims;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Hypixel;
using FastEndpoints.Security;
using FastEndpoints.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Npgsql;
using NSubstitute;
using StackExchange.Redis;

namespace EliteAPI.Tests.Jacob;

/// <summary>
/// App fixture with PostgreSQL testcontainer for Jacob Leaderboard integration tests.
/// </summary>
public class JacobTestApp : AppFixture<Program>
{
    private PostgreSqlContainer _postgres = null!;
    private string _jwtSigningKey = null!;
    
    // Test constants
    public const ulong TestGuildId = 200001000000000001;
    public const string TestLeaderboardId = "test-lb-001";
    
    // Primary test user
    public const ulong TestUserId = 100001000000000001;
    public const string TestPlayerUuid = "testuuid00000001";
    public const string TestPlayerIgn = "TestPlayer";
    
    // Secondary test user (for knockoff tests)
    public const ulong TestUser2Id = 100002000000000002;
    public const string TestPlayer2Uuid = "testuuid00000002";
    public const string TestPlayer2Ign = "TestPlayer2";
    
    // Third test user
    public const ulong TestUser3Id = 100003000000000003;
    public const string TestPlayer3Uuid = "testuuid00000003";
    public const string TestPlayer3Ign = "TestPlayer3";
    
    // Fourth test user (for knockoff)
    public const ulong TestUser4Id = 100004000000000004;
    public const string TestPlayer4Uuid = "testuuid00000004";
    public const string TestPlayer4Ign = "TestPlayer4";
    
    // Admin user for testing manage endpoints
    public const ulong AdminUserId = 100005000000000005;
    
    // Pre-configured clients
    public HttpClient AnonymousClient { get; private set; } = null!;
    public HttpClient BotClient { get; private set; } = null!;
    public HttpClient GuildAdminClient { get; private set; } = null!;

    protected override async ValueTask PreSetupAsync()
    {
        Environment.SetEnvironmentVariable("DISCORD_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("DISCORD_CLIENT_SECRET", "test-client-secret");
        Environment.SetEnvironmentVariable("DISCORD_BOT_TOKEN", "test-bot-token");
        Environment.SetEnvironmentVariable("HYPIXEL_API_KEY", "test-api-key");
        
        _postgres = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("eliteapi_jacob_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
        
        await _postgres.StartAsync();
    }

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DataContext>));
        if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);
        
        var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(System.Data.Common.DbConnection));
        if (dbConnectionDescriptor != null) services.Remove(dbConnectionDescriptor);
        
        var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DataContext));
        if (contextDescriptor != null) services.Remove(contextDescriptor);
        
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_postgres.GetConnectionString());
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();
        
        services.AddDbContext<DataContext>(options =>
        {
            options.UseNpgsql(dataSource);
        });

        services.Configure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        });

        services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
            Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters.ValidIssuer = "eliteapi";
            options.TokenValidationParameters.ValidAudience = "eliteapi";
            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidateLifetime = true;
            options.TokenValidationParameters.IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(_jwtSigningKey));
        });

        // Mock Redis
        var mockRedis = Substitute.For<IConnectionMultiplexer>();
        var mockDb = Substitute.For<IDatabase>();
        mockDb.LockTakeAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(true));
        mockRedis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(mockDb);
        services.RemoveAll<IConnectionMultiplexer>();
        services.AddSingleton(mockRedis);

        // Mock DiscordService
        services.RemoveAll<IDiscordService>();
        services.AddScoped<IDiscordService>(sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var mock = Substitute.For<IDiscordService>();

            // Setup GetGuild to fetch from DB to ensure state consistency
            mock.GetGuild(Arg.Any<ulong>()).Returns(async callInfo =>
            {
                var id = callInfo.Arg<ulong>();
                return await db.Guilds.FirstOrDefaultAsync(g => g.Id == id);
            });
            
            mock.GetGuildMemberIfAdmin(Arg.Any<ClaimsPrincipal>(), Arg.Any<ulong>(), Arg.Any<GuildPermission>())
                .Returns(callInfo =>
                {
                    var user = callInfo.Arg<ClaimsPrincipal>();
                    var guildId = callInfo.Arg<ulong>();
                    
                    // Just look for the Admin role value in any claim for testing purposes
                    if (user.Claims.Any(c => c.Value == ApiUserPolicies.Admin))
                    {
                        return Task.FromResult<GuildMember?>(new GuildMember
                        {
                            AccountId = AdminUserId.ToString(),
                            GuildId = guildId,
                            Permissions = 8 // Administrator
                        });
                    }
                    return Task.FromResult<GuildMember?>(null);
                });

            return mock;
        });
    }

    protected override async ValueTask SetupAsync()
    {
        var configuration = Services.GetRequiredService<IConfiguration>();
        _jwtSigningKey = configuration["Jwt:Secret"] 
            ?? throw new InvalidOperationException("Jwt:Secret not found in configuration");
        
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        await db.Database.MigrateAsync();
        
        await SeedTestData(db);
        
        AnonymousClient = CreateClient();
        
        BotClient = CreateClient(c =>
        {
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", "EliteDiscordBot test-bot-token");
        });
        
        GuildAdminClient = CreateClient(c =>
        {
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", GenerateJwtToken(AdminUserId.ToString(), "testadmin", 
                    ApiUserPolicies.User, ApiUserPolicies.Admin));
        });
    }

    public async Task<DataContext> GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DataContext>();
    }

    private async Task SeedTestData(DataContext db)
    {
        if (await db.Accounts.AnyAsync(a => a.Id == TestUserId))
            return;
        
        // Create test accounts
        await CreateTestUser(db, TestUserId, TestPlayerUuid, TestPlayerIgn, "testuser", 600000);
        await CreateTestUser(db, TestUser2Id, TestPlayer2Uuid, TestPlayer2Ign, "testuser2", 500000);
        await CreateTestUser(db, TestUser3Id, TestPlayer3Uuid, TestPlayer3Ign, "testuser3", 400000);
        await CreateTestUser(db, TestUser4Id, TestPlayer4Uuid, TestPlayer4Ign, "testuser4", 300000);
        
        // Create admin account (no player data needed, just for auth)
        var adminAccount = new EliteAccount
        {
            Id = AdminUserId,
            DisplayName = "TestAdmin",
            Username = "testadmin",
            Permissions = PermissionFlags.Admin
        };
        db.Accounts.Add(adminAccount);
        
        var adminApiUser = new ApiUser
        {
            Id = AdminUserId.ToString(),
            UserName = "testadmin",
            NormalizedUserName = "TESTADMIN",
            Email = "testadmin@test.local",
            NormalizedEmail = "TESTADMIN@TEST.LOCAL",
            AccountId = AdminUserId
        };
        db.Users.Add(adminApiUser);
        
        // Create test guild with Jacob leaderboard feature
        var guild = new Guild
        {
            Id = TestGuildId,
            Name = "Test Guild",
            HasBot = true,
            Features = new GuildFeatures
            {
                JacobLeaderboardEnabled = true,
                JacobLeaderboard = new GuildJacobLeaderboardFeature
                {
                    MaxLeaderboards = 5,
                    Leaderboards = [
                        new GuildJacobLeaderboard
                        {
                            Id = TestLeaderboardId,
                            Title = "Test Leaderboard",
                            Active = true,
                            Crops = new CropRecords()
                        }
                    ]
                }
            }
        };
        db.Guilds.Add(guild);
        
        await db.SaveChangesAsync();
    }

    private async Task CreateTestUser(DataContext db, ulong userId, string playerUuid, string ign, string username, int collected)
    {
        var account = new EliteAccount
        {
            Id = userId,
            DisplayName = ign,
            Username = username,
            Permissions = PermissionFlags.None
        };
        db.Accounts.Add(account);
        
        var mcAccount = new MinecraftAccount
        {
            Id = playerUuid,
            Name = ign,
            AccountId = userId,
            Selected = true
        };
        db.MinecraftAccounts.Add(mcAccount);
        
        var profile = new Profile
        {
            ProfileId = $"profile-{playerUuid}",
            ProfileName = "Test",
            IsDeleted = false
        };
        db.Profiles.Add(profile);
        
        var profileMember = new ProfileMember
        {
            Id = Guid.NewGuid(),
            PlayerUuid = playerUuid,
            ProfileId = profile.ProfileId,
            Profile = profile,
            IsSelected = true,
            LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        db.ProfileMembers.Add(profileMember);
        
        var jacobContest = new JacobContest
        {
            Id = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)userId,
            Crop = Crop.Wheat,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600,
            Participants = 100
        };
        db.JacobContests.Add(jacobContest);
        
        var jacobData = new JacobData
        {
            ProfileMemberId = profileMember.Id,
            Participations = 1,
            Contests = [
                new ContestParticipation
                {
                    ProfileMemberId = profileMember.Id,
                    ProfileMember = profileMember,
                    JacobContestId = jacobContest.Id,
                    JacobContest = jacobContest,
                    Collected = collected,
                    Position = 1,
                    MedalEarned = ContestMedal.Gold
                }
            ]
        };
        profileMember.JacobData = jacobData;
    }

    protected override async ValueTask TearDownAsync()
    {
        AnonymousClient?.Dispose();
        BotClient?.Dispose();
        GuildAdminClient?.Dispose();

        await _postgres.DisposeAsync();
    }

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
}
