using System.Globalization;
using System.Text;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Account.UpdateSettings;

internal sealed class UpdateAccountEndpoint(
	IAccountService accountService
) : Endpoint<UpdateUserSettingsDto> {
	
	public override void Configure() {
		Patch("/account/settings");
		Version(0);

		Summary(s => {
			s.Summary = "Update Account Settings";
		});
	}

	public override async Task HandleAsync(UpdateUserSettingsDto request, CancellationToken c) {
		var id = User.GetDiscordId();
		if (id is null) {
			ThrowError("Unauthorized", StatusCodes.Status401Unauthorized);
		}
        
		var result = await accountService.UpdateSettings(id.Value, request);

		if (result is BadRequestObjectResult bad) {
			ThrowError(bad.Value?.ToString() ?? "Bad request", StatusCodes.Status400BadRequest);
		}

		await SendNoContentAsync(cancellation: c);
	}
}

internal sealed class UpdateUserSettingsDtoValidator : Validator<UpdateUserSettingsDto> {
	public UpdateUserSettingsDtoValidator()
	{
		RuleFor(x => x.Suffix)
			.Must(IsEmoji)
			.WithMessage("Invalid Emoji provided!");
	}

	private static bool IsEmoji(string? input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return true; // Null doesn't change the suffix, empty clears it
		}

		// Ensure string is one grapheme cluster
		var stringInfo = new StringInfo(input);
		if (stringInfo.LengthInTextElements != 1)
		{
			return false;
		}

		// Check that all runes are emoji related
		return input.EnumerateRunes().Any(IsRuneAnEmoji);
	}
	
	private static bool IsRuneAnEmoji(Rune rune)
	{
		// Rough check for emoji ranges
		return rune.Value is >= 0x1F600 and <= 0x1F64F || // Emoticons
		       rune.Value is >= 0x1F300 and <= 0x1F5FF || // Misc Symbols and Pictographs
		       rune.Value is >= 0x1F680 and <= 0x1F6FF || // Transport and Map Symbols
		       rune.Value is >= 0x1FA70 and <= 0x1FAFF || // Symbols and Pictographs Extended-A
		       rune.Value is >= 0x2600 and <= 0x26FF  || // Miscellaneous Symbols
		       rune.Value is >= 0x2700 and <= 0x27BF  || // Dingbats
		       rune.Value is >= 0xFE00 and <= 0xFE0F  || // Variation Selectors
		       rune.Value is >= 0x1F900 and <= 0x1F9FF || // Supplemental Symbols and Pictographs
		       rune.Value is >= 0x1F1E6 and <= 0x1F1FF || // Regional Indicator Symbols
		       (rune.Value == 0x200D); // Zero Width Joiner
	}
}