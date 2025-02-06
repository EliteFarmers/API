using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EliteAPI.Data;
using EliteAPI.Parsers.Farming;
using FastEndpoints;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Features.Contests.UploadCurrentContests;

internal sealed class UploadCurrentContestsEndpoint(
    DataContext context,
    IConnectionMultiplexer cache)
	: Endpoint<UploadCurrentContestsRequest>
{
    private const int RequiredIdenticalContestSubmissions = 5;
    
	public override void Configure() {
		Post("/contests/at/now");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Upload upcoming contests for the current SkyBlock year";
			s.Description = "Data used and provided by <see href=\"https://github.com/hannibal002/SkyHanni/\">SkyHanni</see> to display upcoming contests in-game.";
            s.ExampleRequest = new UploadCurrentContestsRequest() {
                Contests = new Dictionary<long, List<string>> {
                    { 1738390500, ["Cactus", "Carrot", "Melon"] },
                    { 1738394100, ["Mushroom", "Nether Wart", "Pumpkin"] },
                    { 1738397700, ["Cocoa Beans", "Potato", "Wheat"] },
                    { 1738401300, ["Cactus", "Cocoa Beans", "Mushroom"] },
                    { 1738404900, ["Carrot", "Nether Wart", "Wheat"] }
                }
            };
        });
	}

	public override async Task HandleAsync(UploadCurrentContestsRequest request, CancellationToken ct) {
		var currentDate = new SkyblockDate(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var currentYear = currentDate.Year;
        
        var db = cache.GetDatabase();
        
        if (currentDate.Month > 8) {
            ThrowError("Contests cannot be submitted this late in the year!");
        }
        
        var body = request.Contests;
        var bodyKeys = body.Keys.Distinct().OrderBy(k => k).ToList();
        
        if (bodyKeys.Count != 124) {
            ThrowError("Invalid number of contests! Expected 124, got " + bodyKeys.Count);
        }

        var firstKey = bodyKeys.First();

        // Change unix milliseconds to seconds
        if (firstKey > 1000000000000) {
            body = new Dictionary<long, List<string>>();
            foreach (var key in bodyKeys) {
                body[key / 1000] = request.Contests[key];
            }
            
            bodyKeys = body.Keys.Distinct().OrderBy(k => k).ToList();
        }
        
        // Check if any of the timestamps are invalid
        if (bodyKeys.Exists(timestamp => !timestamp.IsValidJacobContestTime(currentYear)))
        {
            ThrowError("Invalid timestamp! All contests must be from the current SkyBlock year (" +
                              (currentYear + 1) + ") and begin on the 15 minute mark!");
        }
        
        // Check if any of the crops are invalid
        if (body.Values.ToList().Exists(crops => // Check that all crops are valid and that there are no duplicates
                crops.Distinct().Count() != 3 ||
                crops.Exists(crop => FormatUtils.FormattedCropNameToCrop(crop) is null))) 
        {
            ThrowError("Invalid contest(s)! All crops must be valid without duplicates in the same contest!");
        }
        
        var lastYearKey = $"last-contests:{currentYear - 1}";
        if (!await db.KeyExistsAsync(lastYearKey)) {
            var start = new SkyblockDate(currentYear - 1, 0, 0).UnixSeconds;
            var end = new SkyblockDate(currentYear, 0, 0).UnixSeconds;
            
            var contests = await context.JacobContests
                .Where(j => j.Timestamp >= start && j.Timestamp < end)
                .GroupBy(j => j.Timestamp)
                .Select(j => new {
                    Timestamp = j.Key,
                    Crops = j.Select(c => c.Crop.ProperName()).ToList()
                })
                .ToDictionaryAsync(k => k.Timestamp, v => v.Crops, cancellationToken: ct);
            
            var serializedContests = JsonSerializer.Serialize(contests);
            await db.StringSetAsync(lastYearKey, serializedContests, TimeSpan.FromDays(5));
        }

        try {
            var lastYearContests = await db.StringGetAsync(lastYearKey);
            var lastYearContestsParsed = JsonSerializer.Deserialize<Dictionary<long, List<string>>>(lastYearContests!);

            if (lastYearContestsParsed is not null && lastYearContestsParsed.Count > 0) {
                // Check to make sure that the contests are NOT the same as last year
                
                var lastValues = lastYearContestsParsed
                    .OrderBy(k => k.Key)
                    .Select(k => k.Value)
                    .ToList();
                var currentValues = body
                    .OrderBy(k => k.Key)
                    .Select(k => k.Value)
                    .ToList();

                var allEqual = true;
                
                for (var i = 0; i < lastValues.Count; i++) {
                    var lastValue = lastValues[i];
                    var currentValue = currentValues[i];

                    // Continue looking if the lists are the same
                    if (!lastValue.Except(currentValue).Any()) continue;
                    
                    // Different lists, break out of the loop
                    allEqual = false;
                    break;
                }
                
                if (allEqual) {
                    ThrowError("Contests are the same as last year!");
                }
            }
        } catch (Exception e) {
            Logger.LogError(e, "Failed to deserialize last year's contests");
        }

        var httpContext = HttpContext.Request.HttpContext;
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ipAddress))
        {
            ThrowError("Invalid request");
        }

        var addressKey = IPAddress.IsLoopback(httpContext.Connection.RemoteIpAddress!) 
            ? $"contestsSubmission:{Guid.NewGuid()}" // Use a GUID for localhost so that it can be tested
            : $"contestsSubmission:{ipAddress}";

        var existingData = await db.StringGetAsync(addressKey);
        
        // Check if IP has already submitted a response
        if (!string.IsNullOrEmpty(existingData))
        {
            ThrowError("Already submitted a response");
        }
        
        // Store that the IP has submitted a response
        await db.StringSetAsync(addressKey, "1", TimeSpan.FromHours(1));
        
        // Serialize the body to a JSON string
        var serializedData = JsonSerializer.Serialize(body);
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(serializedData)));

        // Increment the number of this particular response
        var hashKey = $"contestsHash:{hash}";
        await db.StringIncrementAsync(hashKey);
        await db.StringGetSetExpiryAsync(hashKey, TimeSpan.FromHours(5));
        
        // Get the current number of this particular response
        var identicalResponses = await db.StringGetAsync(hashKey);

        if (!identicalResponses.TryParse(out long val) || val < RequiredIdenticalContestSubmissions) {
            await SendNoContentAsync(ct);
            return;
        }
        
        var secondsUntilNextYear = FormatUtils.GetTimeFromSkyblockDate(currentYear + 1, 0, 0) - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // Save the request data
        await db.StringSetAsync($"contests:{currentYear}", serializedData, TimeSpan.FromSeconds(secondsUntilNextYear));

		await SendNoContentAsync(ct);
	}
}