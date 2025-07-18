using EliteAPI.Configuration.Settings;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Weight.GetAllWeights;

internal sealed class GetAllWeightsEndpoint(IOptions<ConfigFarmingWeightSettings> weightSettings) : EndpointWithoutRequest<WeightsDto> {
	
	private readonly ConfigFarmingWeightSettings _weightSettings = weightSettings.Value;
	
	public override void Configure() {
		Get("/weights/all");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get all weight constants";
			s.Description = "Get all farming weight constants";
		});

		Description(d => d.AutoTagOverride("Weight"));
		Options(opt => opt.CacheOutput(CachePolicy.Hours));
	}

	public override async Task HandleAsync(CancellationToken c) {
		var rawWeights = _weightSettings.CropsPerOneWeight;
		var crops = new Dictionary<string, double>();
        
		foreach (var (key, value) in rawWeights) {
			var formattedKey = FormatUtils.GetFormattedCropName(key);
			if (formattedKey is null) continue;
            
			crops.TryAdd(formattedKey, value);
		}
        
		var reversed = _weightSettings.PestDropBrackets
			.DistinctBy(p => p.Value)
			.ToDictionary(pair => pair.Value, pair => pair.Key);

		var result = new WeightsDto {
			Crops = crops,
			Pests = {
				Brackets = _weightSettings.PestDropBrackets,
				Values = _weightSettings.PestCropDropChances
					.DistinctBy(p => p.Key.ToString().ToLowerInvariant())
					.ToDictionary(
						pair => pair.Key.ToString().ToLowerInvariant(), 
						pair => pair.Value.GetPrecomputed().ToDictionary(
							valuePair => reversed[valuePair.Key], 
							valuePair => valuePair.Value
						)
					)
			}
		};

		await SendAsync(result, cancellation: c);
	}
}