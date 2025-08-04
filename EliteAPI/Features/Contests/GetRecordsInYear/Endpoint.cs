using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Utilities;

namespace EliteAPI.Features.Contests.GetRecordsInYear;

internal sealed class GetRecordsInYearEndpoint(
	IContestsService contestsService)
	: Endpoint<SkyBlockYearRequest, YearlyCropRecordsDto> 
{
	public override void Configure() {
		Get("/contests/records/{Year:int}");
		AllowAnonymous();
		ResponseCache(600);
		
		Summary(s => {
			s.Summary = "Get contest records for a SkyBlock year";
		});
		
		Options(o => o.CacheOutput(b => b.Expire(TimeSpan.FromMinutes(20))));
	}

	public override async Task HandleAsync(SkyBlockYearRequest request, CancellationToken ct) {
		var startTime = FormatUtils.GetTimeFromSkyblockDate(request.Year - 1, 0, 0);
		var endTime = FormatUtils.GetTimeFromSkyblockDate(request.Year, 0, 0);
        
		if (startTime > SkyblockDate.Now.UnixSeconds) {
			ThrowError("Cannot fetch records for a year that hasn't happened yet!");
		}
        
		var result = new YearlyCropRecordsDto {
			Year = request.Year,
			Crops = new Dictionary<string, List<ContestParticipationWithTimestampDto>> {
				{ "cactus", await contestsService.FetchRecords(Crop.Cactus, startTime, endTime) },
				{ "carrot", await contestsService.FetchRecords(Crop.Carrot, startTime, endTime) },
				{ "potato", await contestsService.FetchRecords(Crop.Potato, startTime, endTime) },
				{ "pumpkin", await contestsService.FetchRecords(Crop.Pumpkin, startTime, endTime) },
				{ "melon", await contestsService.FetchRecords(Crop.Melon, startTime, endTime) },
				{ "mushroom", await contestsService.FetchRecords(Crop.Mushroom, startTime, endTime) },
				{ "cocoa", await contestsService.FetchRecords(Crop.CocoaBeans, startTime, endTime) },
				{ "cane", await contestsService.FetchRecords(Crop.SugarCane, startTime, endTime) },
				{ "wart", await contestsService.FetchRecords(Crop.NetherWart, startTime, endTime) },
				{ "wheat", await contestsService.FetchRecords(Crop.Wheat, startTime, endTime) }
			}
		};
		
		await Send.OkAsync(result, cancellation: ct);
	}
}