using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Contests;

public class SkyBlockYearRequest {
	/// <summary>
	/// SkyBlock year
	/// </summary>
	public int Year { get; set; }
}

public class SkyBlockMonthRequest : SkyBlockYearRequest {
	/// <summary>
	/// SkyBlock month
	/// </summary>
	public int Month { get; set; }
}

public class SkyBlockDayRequest : SkyBlockMonthRequest {
	/// <summary>
	/// SkyBlock day
	/// </summary>
	public int Day { get; set; }
}

internal sealed class SkyBlockYearRequestValidator : Validator<SkyBlockYearRequest> {
	public SkyBlockYearRequestValidator() {
		RuleFor(r => r.Year)
			.GreaterThan(0)
			.WithMessage("Year must be greater than 0");
	}
}

internal sealed class SkyBlockMonthRequestValidator : Validator<SkyBlockMonthRequest> {
	public SkyBlockMonthRequestValidator() {
		Include(new SkyBlockYearRequestValidator());
		RuleFor(r => r.Month)
			.GreaterThan(0)
			.LessThan(13)
			.WithMessage("Month must be between 1 and 12 inclusive");
	}
}

internal sealed class SkyBlockDayRequestValidator : Validator<SkyBlockDayRequest> {
	public SkyBlockDayRequestValidator() {
		Include(new SkyBlockMonthRequestValidator());
		RuleFor(r => r.Day)
			.GreaterThan(0)
			.LessThan(32)
			.WithMessage("Day must be between 1 and 31 inclusive");
	}
}