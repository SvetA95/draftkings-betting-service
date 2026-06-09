using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class MarketSuspensionTests : PlaywrightTest
{
    private IAPIRequestContext _api = null!;
    private readonly string _baseUrl = "http://localhost:5000";

    [SetUp]
    public async Task SetUp()
    {
        _api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl
        });
    }

    [TearDown]
    public async Task TearDown()
    {
        await _api.DisposeAsync();
    }

    [Test]
    public async Task Suspended_Market_Rejects_New_Bets()
    {
        // Arrange: create user
        var userResponse = await _api.PostAsync("/api/users", new()
        {
            DataObject = new { name = $"user_{Guid.NewGuid():N}", balance = 100.00m }
        });
        var user = await userResponse.JsonAsync();
        var userId = user.Value.GetProperty("id").GetString();

        // Arrange: create market
        var eventId = Guid.NewGuid();
        var marketResponse = await _api.PostAsync("/api/markets", new()
        {
            DataObject = new
            {
                name = "Match Winner",
                eventId,
                eventName = "Test Match",
                selections = new[] { new { name = "Team A", odds = 2.50 } }
            }
        });
        var market = await marketResponse.JsonAsync();
        var marketId = market.Value.GetProperty("id").GetString();
        var selectionId = market.Value.GetProperty("selections")[0].GetProperty("id").GetString();

        // Act: suspend the market
        await _api.PostAsync($"/api/markets/{marketId}/suspend", new() { });

        // Wait until market is actually suspended (async propagation)
        var isSuspended = await PollUntilAsync(async () =>
        {
            var state = await _api.GetAsync($"/api/markets/{marketId}");
            var data = await state.JsonAsync();
            return data.Value.GetProperty("state").GetString() == "Suspended";
        }, timeoutMs: 5000);

        Assert.That(isSuspended, Is.True, "Market should transition to Suspended state");

        // Act: attempt to place a bet on suspended market
        var betResponse = await _api.PostAsync($"/api/users/{userId}/bets", new()
        {
            DataObject = new { selectionId, stake = 10.00m }
        });

        // Assert: bet should be rejected
        Assert.That(betResponse.Status, Is.EqualTo(409));

        // Assert: balance unchanged
        var balanceResponse = await _api.GetAsync($"/api/users/{userId}/balance");
        var balance = await balanceResponse.JsonAsync();
        Assert.That(balance.Value.GetProperty("amount").GetDecimal(), Is.EqualTo(100.00m));
    }

    private static async Task<bool> PollUntilAsync(Func<Task<bool>> condition, int timeoutMs, int intervalMs = 250)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return true;
            await Task.Delay(intervalMs);
        }
        return false;
    }
}