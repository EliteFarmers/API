using FastEndpoints;

namespace EliteAPI.Features.Recap.Commands;

public class GenerateGlobalStatsCommand : ICommand
{
    public int Year { get; set; }
}
