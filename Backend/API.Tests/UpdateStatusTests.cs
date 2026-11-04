using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.DTOs;
using Common.Models;

namespace API.Tests;

[Collection("Api")]
public class UpdateStatusTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;
    public UpdateStatusTests(ApiFactory factory) => _client = factory.CreateClient();

    private async Task<RequestDto> FindByTitleAsync(string title)
    {
        var response = await _client.GetAsync($"/api/requests?search={Uri.EscapeDataString(title)}");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<RequestDto>>(Json);
        return page!.Items.Single(r => r.Title == title);
    }

    [Fact]
    public async Task UpdateStatus_ValidChange_UpdatesRequestAndWritesHistory()
    {
        var before = await FindByTitleAsync("Update target");
        var body = new { newStatus = "InProgress", version = before.Version, changedBy = "tester" };

        var response = await _client.PatchAsJsonAsync($"/api/requests/{before.Id}/status", body, Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<RequestDto>(Json);
        Assert.Equal(RequestStatus.InProgress, updated!.Status);
        Assert.NotEqual(before.Version, updated.Version);
        Assert.True(updated.UpdatedAt > before.UpdatedAt);

        var history = await _client.GetFromJsonAsync<List<RequestStatusHistoryDto>>(
            $"/api/requests/{before.Id}/history", Json);
        var entry = Assert.Single(history!);
        Assert.Equal(RequestStatus.New, entry.PreviousStatus);
        Assert.Equal(RequestStatus.InProgress, entry.NewStatus);
        Assert.Equal("tester", entry.ChangedBy);
    }
    [Fact]
    public async Task UpdateStatus_ConcurrentUpdates_OnlyOneSucceedsOtherGetsConflict()
    {
        var target = await FindByTitleAsync("Concurrency target");

        var toInProgress = new { newStatus = "InProgress", version = target.Version, changedBy = "user-a" };
        var toWaiting = new { newStatus = "Waiting", version = target.Version, changedBy = "user-b" };

        var responses = await Task.WhenAll(
            _client.PatchAsJsonAsync($"/api/requests/{target.Id}/status", toInProgress, Json),
            _client.PatchAsJsonAsync($"/api/requests/{target.Id}/status", toWaiting, Json));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);

        var history = await _client.GetFromJsonAsync<List<RequestStatusHistoryDto>>(
            $"/api/requests/{target.Id}/history", Json);
        Assert.Single(history!);
    }
    [Fact]
    public async Task UpdateStatus_NonExistingRequest_Returns404()
    {
        var body = new
        {
            newStatus = "InProgress",
            version = Convert.ToBase64String(new byte[8]),
            changedBy = "tester"
        };

        var response = await _client.PatchAsJsonAsync("/api/requests/999999/status", body, Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_NonExistingRequest_Returns404()
    {
        var response = await _client.GetAsync("/api/requests/999999/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    [Fact]
    public async Task UpdateStatus_InvalidTransition_Returns400AndWritesNoHistory()
    {
        var completed = await FindByTitleAsync("Delta update");
        var body = new { newStatus = "InProgress", version = completed.Version, changedBy = "tester" };

        var response = await _client.PatchAsJsonAsync($"/api/requests/{completed.Id}/status", body, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Cannot change status", content);

        var history = await _client.GetFromJsonAsync<List<RequestStatusHistoryDto>>(
            $"/api/requests/{completed.Id}/history", Json);
        Assert.Empty(history!);
    }
}