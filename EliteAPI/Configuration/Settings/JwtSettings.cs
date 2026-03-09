namespace EliteAPI.Configuration.Settings;

public class JwtSettings
{
	public const string SectionName = "Jwt";

	public string Secret { get; set; } = string.Empty;
	public string Issuer { get; set; } = "eliteapi";
	public string Audience { get; set; } = "eliteapi";
	public int TokenExpirationInMinutes { get; set; } = 60;
	public int RefreshTokenExpirationInDays { get; set; } = 30;
}
