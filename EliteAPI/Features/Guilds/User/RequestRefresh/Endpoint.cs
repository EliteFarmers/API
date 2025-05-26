using EliteAPI.Authentication;
using EliteAPI.Configuration.Settings;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Guilds.User.RequestRefresh;

internal sealed class RequestGuildRefreshEndpoint(
    IOptions<ConfigCooldownSettings> cooldownSettings,
    IDiscordService discordService)
    : Endpoint<DiscordIdRequest> 
{
    private readonly ConfigCooldownSettings _cooldowns = cooldownSettings.Value;
    
    public override void Configure() {
        Post("/user/guild/{DiscordId}/refresh");
        Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
        Version(0);
		
        Description(s => s.Accepts<DiscordIdRequest>());
		
        Summary(s => {
            s.Summary = "Request Guild Refresh";
            s.Description = "This fetches the latest data from Discord for the specified guild";
        });
    }

    public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) 
    {
        await discordService.RefreshDiscordGuild(request.DiscordIdUlong, replaceImages: true, cooldown: _cooldowns.DiscordGuildCooldown);
        await SendNoContentAsync(cancellation: c);
    }
}