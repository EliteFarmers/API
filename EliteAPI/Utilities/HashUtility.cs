namespace EliteAPI.Utilities;

public static class HashUtility
{
	public static string ComputeSha256Hash(string rawData)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawData));
        var builder = new System.Text.StringBuilder();
        foreach (var t in bytes)
        {
            builder.Append(t.ToString("x2"));
        }
        return builder.ToString();
    }
}