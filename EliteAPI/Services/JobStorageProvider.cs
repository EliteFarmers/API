using System.Text.Json.Serialization;
using FastEndpoints;
using StackExchange.Redis;

namespace EliteAPI.Services;

public sealed class JobRecord : IJobStorageRecord
{
	public string QueueID { get; set; } = null!;
	public Guid TrackingID { get; set; } = Guid.Empty!;
	[JsonIgnore]
	public object Command { get; set; } = null!;
	public string CommandTypeName { get; set; } = null!;
	public string SerializedCommand { get; set; } = null!;
	public DateTime ExecuteAfter { get; set; }
	public DateTime ExpireOn { get; set; }
	public bool IsComplete { get; set; }
	public bool IsCancelled { get; set; }
}

public class JobStorageProvider(IConnectionMultiplexer redis, ILogger<JobStorageProvider> logger) : IJobStorageProvider<JobRecord>
{
	public Task StoreJobAsync(JobRecord r, CancellationToken ct) {
       var db = redis.GetDatabase();
       var key = $"job:{r.TrackingID}";

       // Manually serialize the command and store its type name.
       r.CommandTypeName = r.Command.GetType().AssemblyQualifiedName!;
       r.SerializedCommand = System.Text.Json.JsonSerializer.Serialize(r.Command);

       var value = System.Text.Json.JsonSerializer.Serialize(r);
       return db.StringSetAsync(key, value, TimeSpan.FromHours(1));
    }

    public Task<IEnumerable<JobRecord>> GetNextBatchAsync(PendingJobSearchParams<JobRecord> parameters) {
       var db = redis.GetDatabase();
       var server = redis.GetServer(redis.GetEndPoints().First());
       
       var keys = server.Keys(pattern: "job:*");
       var records = new List<JobRecord>();
       
       var matchFunc = parameters.Match.Compile();

       foreach (var key in keys) {
	       if (records.Count >= parameters.Limit) break;
	       
          var value = db.StringGet(key);
          if (!value.HasValue) continue;
          
          try {
             var record = System.Text.Json.JsonSerializer.Deserialize<JobRecord>(value.ToString());
             if (record != null && matchFunc(record)) {
                // Find the type from the stored type name.
                var commandType = Type.GetType(record.CommandTypeName);

                if (commandType is null) {
                    logger.LogWarning("Could not find command type '{TypeName}' for job {TrackingID}. Skipping", record.CommandTypeName, record.TrackingID);
                    continue;
                }

                // Deserialize the command string into the correct type.
                record.Command = System.Text.Json.JsonSerializer.Deserialize(record.SerializedCommand, commandType)!;
                records.Add(record);
             }
          }
          catch (Exception ex) {
             logger.LogError(ex, "Failed to deserialize job from key {Key}. The key will be deleted", key);
             db.KeyDelete(key); // Delete corrupted/invalid keys
          }
       }
       return Task.FromResult<IEnumerable<JobRecord>>(records);
    }
	
	public Task MarkJobAsCompleteAsync(JobRecord r, CancellationToken ct) {
		var db = redis.GetDatabase();
		var key = $"job:{r.TrackingID}";
		return db.KeyDeleteAsync(key);
	}
	
	public Task CancelJobAsync(Guid trackingId, CancellationToken ct) {
		var db = redis.GetDatabase();
		var key = $"job:{trackingId}";
		return db.KeyDeleteAsync(key);
	}
	
	public async Task OnHandlerExecutionFailureAsync(JobRecord r, Exception exception, CancellationToken ct) {
		var db = redis.GetDatabase();
		var key = $"failed-job:{r.TrackingID}";
		var count = await db.StringIncrementAsync(key);
		if (count >= 3) {
			logger.LogError(exception, "Job {TrackingID} failed {Count} times and will be removed", r.TrackingID, count);
			await db.KeyDeleteAsync($"job:{r.TrackingID}");
		}
		// Doing nothing retries the job
	}
	
	public Task PurgeStaleJobsAsync(StaleJobSearchParams<JobRecord> parameters) {
		// Redis keys have expiration, so no need to purge
		return Task.CompletedTask;
	}
}