using System.Net;
using EliteAPI.Features.Guides.Endpoints;
using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Models.Dtos;
using FastEndpoints;
using FastEndpoints.Testing;
using Shouldly;

namespace EliteAPI.Tests.Guides;

[Collection<GuidesTestCollection>]
public class ListGuidesTests(GuideTestApp App) : TestBase
{
    [Fact]
    public async Task ListGuides_NegativePage_ReturnsFirstPage()
    {
        // Request with negative page
        var (rsp, res) = await App.AnonymousClient.GETAsync<ListGuidesEndpoint, ListGuidesRequest, List<GuideDto>>(
            new ListGuidesRequest { Page = -5, PageSize = 10 });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldNotBeNull();
    }

    [Fact]
    public async Task ListGuides_ZeroPage_ReturnsFirstPage()
    {
         // Request with zero page
        var (rsp, res) = await App.AnonymousClient.GETAsync<ListGuidesEndpoint, ListGuidesRequest, List<GuideDto>>(
            new ListGuidesRequest { Page = 0, PageSize = 10 });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.ShouldNotBeNull();
    }
}
