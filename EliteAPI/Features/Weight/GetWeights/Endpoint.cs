using EliteAPI.Configuration.Settings;
using EliteAPI.Utilities;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Weight.GetWeights;

[Obsolete("Use /weights/all instead")]
internal sealed class GetWeightsEndpoint(IOptions<ConfigFarmingWeightSettings> weightSettings) : EndpointWithoutRequest<Dictionary<string, double>> {
	
	private readonly ConfigFarmingWeightSettings _weightSettings = weightSettings.Value;
	
	public override void Configure() {
		Get("/weights");
		AllowAnonymous();
		Version(0, deprecateAt: 1);

		Summary(s => {
			s.Summary = "Get crop weight constants";
			s.Description = "Get crop weight constants";
		});
		
		Description(d => d.AutoTagOverride("Weight"));
		Options(opt => opt.CacheOutput(CachePolicy.Hours));
	}

	public override async Task HandleAsync(CancellationToken c) {
		var rawWeights = _weightSettings.CropsPerOneWeight;
        
		var weights = new Dictionary<string, double>();
        
		foreach (var (key, value) in rawWeights) {
			var formattedKey = FormatUtils.GetFormattedCropName(key);
            
			if (formattedKey is null) continue;
            
			weights.Add(formattedKey, value);
		}

		await Send.OkAsync(weights, cancellation: c);
	}
}