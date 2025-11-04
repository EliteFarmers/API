using System.Reflection;
using Quartz;

namespace EliteAPI.Utilities;

public interface ISelfConfiguringJob : IJob
{
	/// <summary>
	/// Configures the job and its triggers with the Quartz scheduler.
	/// </summary>
	static abstract void Configure(IServiceCollectionQuartzConfigurator quartz, ConfigurationManager configuration);
}

public static class QuartzExtensions
{
	public static void AddSelfConfiguringJobs(this IServiceCollectionQuartzConfigurator quartz, ConfigurationManager configuration)
	{
		// Find all types that implement ISelfConfiguringJob
		var jobTypes = Assembly.GetExecutingAssembly().GetTypes()
			.Where(t => t.IsAssignableTo(typeof(ISelfConfiguringJob)) && t is { IsInterface: false, IsAbstract: false });
		
		foreach (var jobType in jobTypes)
		{
			// Find the static 'Configure' method on the job type
			var configureMethod = jobType.GetMethod(nameof(ISelfConfiguringJob.Configure), 
				BindingFlags.Public | BindingFlags.Static);
            
			if (configureMethod == null)
			{
				throw new InvalidOperationException($"Could not find static 'Configure' method on job type '{jobType.Name}'.");
			}

			configureMethod.Invoke(null, [quartz, configuration]);
		}
	}
}