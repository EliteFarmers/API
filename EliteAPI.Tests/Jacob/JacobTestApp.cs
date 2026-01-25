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
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;
using Npgsql;
using NSubstitute;
using StackExchange.Redis;

namespace EliteAPI.Tests.Jacob;

/// <summary>
/// App fixture with PostgreSQL testcontainer for Jacob Leaderboard integration tests.
/// Uses dynamic data generation for test isolation and parallelization.
/// </summary>
public class JacobTestApp : AppFixture<Program>
{
	private PostgreSqlContainer _postgres = null!;
	private string _jwtSigningKey = null!;
	private long _idCounter;

	public static readonly DateTimeOffset ReferenceTime = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
	public FakeTimeProvider TimeProvider { get; } = new(ReferenceTime);

	/// <summary>
	/// Admin user ID for testing manage endpoints.
	/// </summary>
	public const ulong AdminUserId = 100005000000000005;

	public HttpClient AnonymousClient { get; private set; } = null!;
	public HttpClient BotClient { get; private set; } = null!;
	public HttpClient GuildAdminClient { get; private set; } = null!;

	protected override async ValueTask PreSetupAsync() {
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

	protected override void ConfigureApp(IWebHostBuilder builder) {
		builder.UseEnvironment("Testing");
	}

	protected override void ConfigureServices(IServiceCollection services) {
		var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DataContext>));
		if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

		var dbConnectionDescriptor =
			services.SingleOrDefault(d => d.ServiceType == typeof(System.Data.Common.DbConnection));
		if (dbConnectionDescriptor != null) services.Remove(dbConnectionDescriptor);

		var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DataContext));
		if (contextDescriptor != null) services.Remove(contextDescriptor);

		var dataSourceBuilder = new NpgsqlDataSourceBuilder(_postgres.GetConnectionString());
		dataSourceBuilder.EnableDynamicJson();
		var dataSource = dataSourceBuilder.Build();

		services.AddDbContext<DataContext>(options => { options.UseNpgsql(dataSource); });

		// Force default schemes to Bearer
		services.Configure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options => {
			options.DefaultAuthenticateScheme =
				Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme =
				Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
		});

		// Configure validation parameters and add logging
		services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
			Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, options => {
				options.TokenValidationParameters.ValidIssuer = "eliteapi";
				options.TokenValidationParameters.ValidAudience = "eliteapi";
				options.TokenValidationParameters.ValidateIssuer = true;
				options.TokenValidationParameters.ValidateAudience = true;
				options.TokenValidationParameters.ValidateLifetime = true;
				options.TokenValidationParameters.IssuerSigningKey =
					new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
						System.Text.Encoding.UTF8.GetBytes(_jwtSigningKey));
			});

		// Replace TimeProvider with FakeTimeProvider
		services.RemoveAll<TimeProvider>();
		services.AddSingleton<TimeProvider>(TimeProvider);

		// Mock Redis with proper handling for GuildAdminHandler authorization
		var mockRedis = Substitute.For<IConnectionMultiplexer>();
		var mockDb = Substitute.For<IDatabase>();

		// Store cached values in-memory for the mock
		var cache = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

		// Mock both StringGetAsync overloads
		mockDb.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
			.Returns(callInfo => {
				var key = callInfo.Arg<RedisKey>().ToString();
				return cache.TryGetValue(key, out var value) ? new RedisValue(value) : RedisValue.Null;
			});

		// Mock StringSetAsync
		mockDb.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>(),
				Arg.Any<CommandFlags>())
			.Returns(callInfo => {
				var key = callInfo.Arg<RedisKey>().ToString();
				var value = callInfo.Arg<RedisValue>().ToString();
				cache[key] = value;
				return Task.FromResult(true);
			});
		mockDb.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(),
				Arg.Any<When>(), Arg.Any<CommandFlags>())
			.Returns(callInfo => {
				var key = callInfo.Arg<RedisKey>().ToString();
				var value = callInfo.Arg<RedisValue>().ToString();
				cache[key] = value;
				return Task.FromResult(true);
			});

		mockDb.LockTakeAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan>(), Arg.Any<CommandFlags>())
			.Returns(Task.FromResult(true));
		mockDb.LockReleaseAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
			.Returns(Task.FromResult(true));

		// Mock GetDatabase
		mockRedis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(mockDb);
		mockRedis.GetDatabase().Returns(mockDb);
		services.RemoveAll<IConnectionMultiplexer>();
		services.AddSingleton(mockRedis);

		// Mock DiscordService
		services.RemoveAll<IDiscordService>();
		services.AddScoped<IDiscordService>(sp => {
			var db = sp.GetRequiredService<DataContext>();
			var mock = Substitute.For<IDiscordService>();

			mock.GetGuild(Arg.Any<ulong>()).Returns(async callInfo => {
				var id = callInfo.Arg<ulong>();
				return await db.Guilds.FirstOrDefaultAsync(g => g.Id == id);
			});

			mock.GetGuildMemberIfAdmin(Arg.Any<ClaimsPrincipal>(), Arg.Any<ulong>(), Arg.Any<GuildPermission>())
				.Returns(callInfo => {
					var user = callInfo.Arg<ClaimsPrincipal>();
					var guildId = callInfo.Arg<ulong>();

					if (user.Claims.Any(c => c.Value == ApiUserPolicies.Admin)) {
						return Task.FromResult<GuildMember?>(new GuildMember {
							AccountId = AdminUserId.ToString(),
							GuildId = guildId,
							Permissions = 8
						});
					}

					return Task.FromResult<GuildMember?>(null);
				});

			return mock;
		});
	}

	protected override async ValueTask SetupAsync() {
		var configuration = Services.GetRequiredService<IConfiguration>();
		_jwtSigningKey = configuration["Jwt:Secret"]
		                 ?? throw new InvalidOperationException("Jwt:Secret not found in configuration");

		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<DataContext>();
		await db.Database.MigrateAsync();

		await SeedAdminUser(db);

		AnonymousClient = CreateClient();

		BotClient = CreateClient(c => {
			c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
				"Bearer", "EliteDiscordBot test-bot-token");
		});

		GuildAdminClient = CreateClient(c => {
			c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
				"Bearer", GenerateJwtToken(AdminUserId.ToString(), "testadmin",
					ApiUserPolicies.User, ApiUserPolicies.Admin));
		});
	}

	private async Task SeedAdminUser(DataContext db) {
		if (await db.Accounts.AnyAsync(a => a.Id == AdminUserId))
			return;

		var adminAccount = new EliteAccount {
			Id = AdminUserId,
			DisplayName = "TestAdmin",
			Username = "testadmin",
			Permissions = PermissionFlags.Admin
		};
		db.Accounts.Add(adminAccount);

		var adminApiUser = new ApiUser {
			Id = AdminUserId.ToString(),
			UserName = "testadmin",
			NormalizedUserName = "TESTADMIN",
			Email = "testadmin@test.local",
			NormalizedEmail = "TESTADMIN@TEST.LOCAL",
			AccountId = AdminUserId
		};
		db.Users.Add(adminApiUser);

		await db.SaveChangesAsync();
	}

	#region Dynamic Data Generation Helpers

	private ulong NextId() => (ulong)Interlocked.Increment(ref _idCounter) + 200000000000000000;

	/// <summary>
	/// Creates a new Guild with Jacob Leaderboard feature enabled.
	/// </summary>
	public async Task<TestGuild> CreateGuildAsync() {
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<DataContext>();

		var guildId = NextId();
		var leaderboardId = $"lb-{guildId}";

		var guild = new Guild {
			Id = guildId,
			Name = $"Test Guild {guildId}",
			HasBot = true,
			Features = new GuildFeatures {
				JacobLeaderboardEnabled = true,
				JacobLeaderboard = new GuildJacobLeaderboardFeature {
					MaxLeaderboards = 5,
					Leaderboards = [
						new GuildJacobLeaderboard {
							Id = leaderboardId,
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

		return new TestGuild(guildId, leaderboardId);
	}

	/// <summary>
	/// Creates a test user with a linked Minecraft account and Jacob contest participation.
	/// </summary>
	public async Task<TestUser> CreateUserAsync(int collected = 500000) {
		using var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<DataContext>();

		var userId = NextId();
		var playerUuid = $"uuid{userId:X16}";
		var ign = $"Player{userId % 10000}";
		var username = $"user{userId}";

		var account = new EliteAccount {
			Id = userId,
			DisplayName = ign,
			Username = username,
			Permissions = PermissionFlags.None
		};
		db.Accounts.Add(account);

		var mcAccount = new MinecraftAccount {
			Id = playerUuid,
			Name = ign,
			AccountId = userId,
			Selected = true
		};
		db.MinecraftAccounts.Add(mcAccount);

		var profile = new Profile {
			ProfileId = $"profile-{playerUuid}",
			ProfileName = "Test",
			IsDeleted = false
		};
		db.Profiles.Add(profile);

		var contestTimestamp = ReferenceTime.AddHours(-1).ToUnixTimeSeconds();
		var jacobContest = new JacobContest {
			Id = contestTimestamp + (long)(userId % 1000000),
			Crop = Crop.Wheat,
			Timestamp = contestTimestamp,
			Participants = 100
		};
		db.JacobContests.Add(jacobContest);

		var profileMemberId = Guid.NewGuid();

		var jacobData = new JacobData {
			ProfileMemberId = profileMemberId,
			Participations = 1,
			Contests = [
				new ContestParticipation {
					ProfileMemberId = profileMemberId,
					JacobContestId = jacobContest.Id,
					JacobContest = jacobContest,
					Collected = collected,
					Position = 1,
					MedalEarned = ContestMedal.Gold
				}
			]
		};

		var profileMember = new ProfileMember {
			Id = profileMemberId,
			PlayerUuid = playerUuid,
			ProfileId = profile.ProfileId,
			Profile = profile,
			IsSelected = true,
			LastUpdated = ReferenceTime.ToUnixTimeSeconds(),
			JacobData = jacobData
		};

		db.ProfileMembers.Add(profileMember);
		await db.SaveChangesAsync();

		return new TestUser(userId, playerUuid, ign, jacobContest.Timestamp);
	}

	/// <summary>
	/// Creates a full test scenario with a guild, leaderboard, and multiple users.
	/// </summary>
	public async Task<TestScenario> CreateScenarioAsync(int userCount = 4) {
		var guild = await CreateGuildAsync();
		var users = new List<TestUser>();

		var baseCollected = 600000;
		for (var i = 0; i < userCount; i++) {
			var user = await CreateUserAsync(baseCollected - (i * 100000));
			users.Add(user);
		}

		return new TestScenario(guild, users);
	}

	#endregion

	protected override async ValueTask TearDownAsync() {
		AnonymousClient?.Dispose();
		BotClient?.Dispose();
		GuildAdminClient?.Dispose();

		await _postgres.DisposeAsync();
	}

	public string GenerateJwtToken(string userId, string username, params string[] roles) {
		var claims = new List<Claim> {
			new(ClaimNames.Name, username),
			new(ClaimNames.NameId, userId),
			new(ClaimNames.Jti, Guid.NewGuid().ToString()),
			new(ClaimNames.Avatar, ""),
			new(ClaimNames.Ign, "TestPlayer"),
			new(ClaimNames.FormattedIgn, "TestPlayer"),
			new(ClaimNames.Uuid, ""),
			new(ClaimNames.Flags, ""),
		};

		foreach (var role in roles) {
			claims.Add(new Claim(ClaimNames.Role, role));
		}

		var token = JwtBearer.CreateToken(o => {
			o.SigningKey = _jwtSigningKey;
			o.Issuer = "eliteapi";
			o.Audience = "eliteapi";
			o.User.Claims.AddRange(claims);
			o.ExpireAt = DateTime.UtcNow.AddHours(1);
		});

		return token;
	}
}

#region Test Data Records

public record TestGuild(ulong Id, string LeaderboardId);

public record TestUser(ulong Id, string PlayerUuid, string Ign, long ContestTimestamp);

public record TestScenario(TestGuild Guild, List<TestUser> Users)
{
	public TestUser User1 => Users[0];
	public TestUser User2 => Users.Count > 1 ? Users[1] : throw new InvalidOperationException("Not enough users");
	public TestUser User3 => Users.Count > 2 ? Users[2] : throw new InvalidOperationException("Not enough users");
	public TestUser User4 => Users.Count > 3 ? Users[3] : throw new InvalidOperationException("Not enough users");
}

#endregion