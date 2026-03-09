namespace EliteAPI.Configuration.Settings;

public class MinecraftRendererSettings
{
	public const string SectionName = "MinecraftRenderer";

	public bool AcceptEula { get; set; } = false;
	public string Version { get; set; } = "1.21.9";
	public string AssetsPath { get; set; } = string.Empty;

	public string ResolveAssetsPath() {
		if (!string.IsNullOrWhiteSpace(AssetsPath)) {
			return AssetsPath;
		}

		return Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"EliteAPI");
	}
}
