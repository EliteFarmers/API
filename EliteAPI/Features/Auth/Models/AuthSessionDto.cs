using System.Text.Json.Serialization;
using EliteAPI.Features.Confirmations.Models;

namespace EliteAPI.Features.Auth.Models;

public class AuthSessionDto
{
	/// <summary>
	/// Discord user ID
	/// </summary>
	public required string Id { get; set; }

	/// <summary>
	/// Discord username
	/// </summary>
	public required string Username { get; set; }

	/// <summary>
	/// Discord avatar hash
	/// </summary>
	public required string Avatar { get; set; }

	/// <summary>
	/// Primary Minecraft IGN
	/// </summary>
	public required string Ign { get; set; }

	/// <summary>
	/// Formatted Primary Minecraft IGN
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? FIgn { get; set; }

	/// <summary>
	/// Primary Minecraft UUID
	/// </summary>
	public required string Uuid { get; set; }

	/// <summary>
	/// List of user roles
	/// </summary>
	public required string[] Roles { get; set; }
	
	/// <summary>
	/// The pending confirmation for the user, if any
	/// </summary>
	[JsonPropertyName("pending_confirmation")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ConfirmationDto? PendingConfirmation { get; set; }
}