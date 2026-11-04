using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.DTOs;
using Common.Models;

namespace API.Tests;



[Collection("Api")]
public class SearchTests

{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;
    public SearchTests(ApiFactory factory) => _client = factory.CreateClient();
    

    private async Task<PagedResult<RequestDto>> GetAsync(string queryString)
    {
        var response = await _client.GetAsync($"/api/requests?{queryString}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<RequestDto>>(Json))!;
    }

    [Fact]
    public async Task Search_MatchesTitleOrOrganizationPrefix()
    {
        var result = await GetAsync("search=Alpha");

        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, r =>
            Assert.True(r.Title.StartsWith("Alpha") || r.OrganizationName.StartsWith("Alpha")));
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyPage()
    {
        var result = await GetAsync("search=Zzz");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Search_CombinedWithStatusFilter_AppliesBoth()
    {
        var result = await GetAsync("search=Alpha&status=New");

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, r => Assert.Equal(RequestStatus.New, r.Status));
    }

    [Fact]
    public async Task Search_WithPaging_ReturnsOnePageButFullTotal()
    {
        var result = await GetAsync("search=Alpha&page=1&pageSize=1");

        Assert.Single(result.Items);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task Search_PercentSign_IsTreatedAsLiteralText()
    {
        var onlyPercent = await GetAsync("search=%25");      
        var withText = await GetAsync("search=100%25");      

        Assert.Equal(0, onlyPercent.TotalCount);
        Assert.Equal(1, withText.TotalCount);
    }

    [Fact]
    public async Task Search_LongerThan100Chars_Returns400()
    {
        var response = await _client.GetAsync($"/api/requests?search={new string('a', 101)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}