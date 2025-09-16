# Hypixel .NET API

This is a package that contains an HttpClient for interacting with the [Hypixel API](https://developer.hypixel.net/). It is used by the Elite API
which powers https://elitebot.dev.

Not affiliated with Mojang or Hypixel in any way.

## Usage

1. Install the package from NuGet

```
dotnet add package EliteFarmers.HypixelAPI
```

2. Add the Hypixel API client to your service collection:

```csharp
builder.Services.AddHypixelApi(opt => {
    opt.ApiKey = builder.Configuration["HypixelApiKey"] ?? throw new InvalidOperationException("HypixelApiKey is not set");
    opt.UserAgent = "MyApplication (+https://example.com)";
});
```
**Note:** You can apply for an API key [here](https://developer.hypixel.net/). Make sure to keep it secret!

Your API Key limits are picked up automatically from the first request you make. There's an automatic throttling mechanism in place to ensure you don't exceed your limits.

3. Inject the `IHypixelApi` into your class and use it:

```csharp
public class MyService {
    private readonly IHypixelApi _hypixelApiClient;
    
    public MyService(IHypixelApi hypixelApiClient) {
        _hypixelApiClient = hypixelApiClient;
    }
    
    public async Task GetPlayerData(string uuid) {
        var player = await _hypixelApiClient.FetchPlayerAsync(uuid);
        // Do something with the player data
    }
}
```

### Monitoring

There are built in .NET 8+ counters for monitoring the API usage, under `hypixel.api`.

An example use might look like this:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(x =>
    {
        x.AddPrometheusExporter(); // or any other exporter you want
        x.AddMeter("hypixel.api");
    });
```

You will have to set up everything else for this yourself, but the metrics will be available for your setup.

### Supported Endpoints

I have only implemented the endpoints and data that I need for the Elite API. If you need more, feel free to open an issue or a PR.

| Endpoint                  | Method                 | Status  | Notes                                                                              |
|---------------------------|------------------------|---------|------------------------------------------------------------------------------------|
| /player                   | FetchPlayerAsync       | Partial | Not all properties are mapped                                                      |
| /skyblock/profiles        | FetchProfilesAsync     | Partial | Not all properties are mapped                                                      |
| /skyblock/garden          | FetchGardenAsync       | Partial | Most properties are mapped                                                         |
| /skyblock/bazaar          | FetchBazaarAsync       | Full    | All properties are mapped                                                          |
| /skyblock/auctions        | FetchAuctionHouseAsync | Partial | Not all properties are mapped                                                      |
| /skyblock/firesales       | FetchFiresalesAsync    | Full    | All properties are mapped                                                          |
| /resources/skyblock/items | FetchItemsAsync+       | Partial | Most properties are mapped, the rest are passed through with \[JsonExtensionData\] |

## Development

Contributions are welcome! You just need to clone the repo and open it in your IDE of choice.