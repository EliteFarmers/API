using EliteAPI.Configuration.Settings;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Weight.GetWeights;

[Obsolete("Use /weights/all instead")]
sealed class GetWeightsEndpoint(IOptions<ConfigFarmingWeightSettings> weightSettings) : EndpointWithoutRequest<Dictionary<string, double>> {
	
	private readonly ConfigFarmingWeightSettings _weightSettings = weightSettings.Value;
	
	public override void Configure() {
		Get("/weights");
		AllowAnonymous();

		Summary(s => {
			s.Summary = "Get crop weight constants";
			s.Description = "Get crop weight constants";
		});

		Options(opt => opt.CacheOutput(o => o.Expire(TimeSpan.FromMinutes(2))));
	}

	public override async Task HandleAsync(CancellationToken c) {
		var rawWeights = _weightSettings.CropsPerOneWeight;
        
		var weights = new Dictionary<string, double>();
        
		foreach (var (key, value) in rawWeights) {
			var formattedKey = FormatUtils.GetFormattedCropName(key);
            
			if (formattedKey is null) continue;
            
			weights.Add(formattedKey, value);
		}

		await SendAsync(weights, cancellation: c);
	}
}