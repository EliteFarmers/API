using EliteAPI.Configuration.Settings;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Events.Public.GetEventDefaults;

internal sealed class GetEventDefaultsEndpoint : EndpointWithoutRequest<EventDefaultsDto> {
	public override void Configure() {
		Get("/event/defaults");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get event default constants";
			s.Description = "Default constants for event settings.";
		});

		Options(opt => opt.CacheOutput(CachePolicy.Hours));
	}

	public override async Task HandleAsync(CancellationToken c) {
		var result = new EventDefaultsDto {
			CropWeights = FarmingWeightConfig.Settings.EventCropWeights,
			MedalValues = new MedalEventData().MedalWeights,
			PestWeights = new PestEventData().PestWeights
		};

		await Send.OkAsync(result, c);
	}
}