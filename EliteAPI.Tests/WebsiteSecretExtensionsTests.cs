using EliteAPI.Authentication;
using Microsoft.AspNetCore.Http;

namespace EliteAPI.Tests;

public class WebsiteSecretExtensionsTests
{
	[Fact]
	public void HasValidWebsiteSecret_Returns_True_For_Matching_Header() {
		var context = new DefaultHttpContext();
		context.Request.Headers[WebsiteSecretExtensions.WebsiteSecretHeaderName] = "secret123";

		var valid = context.HasValidWebsiteSecret("secret123");

		valid.ShouldBeTrue();
	}

	[Fact]
	public void HasValidWebsiteSecret_Returns_False_For_NonMatching_Header() {
		var context = new DefaultHttpContext();
		context.Request.Headers[WebsiteSecretExtensions.WebsiteSecretHeaderName] = "wrong";

		var valid = context.HasValidWebsiteSecret("secret123");

		valid.ShouldBeFalse();
	}
}
