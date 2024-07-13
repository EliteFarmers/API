using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Accounts;

public class UserSettings {
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	/// <summary>
	///	Selected weight image for the bot
	/// </summary>
	[MaxLength(256)]
	public string? WeightImage { get; set; }
}