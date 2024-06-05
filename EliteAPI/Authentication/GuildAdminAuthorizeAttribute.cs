using Microsoft.AspNetCore.Authorization;

namespace EliteAPI.Authentication;

public class GuildAdminAuthorizeAttribute : AuthorizeAttribute 
{
	public GuildAdminAuthorizeAttribute() 
	{
		Policy = "GuildAdmin";
	}
}