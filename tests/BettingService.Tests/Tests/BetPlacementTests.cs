using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class BetPlacementTests : PlaywrightTest
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
    public async Task Placing_A_Bet_Creates_Active_Bet_And_Deducts_Balance()
    {
        // Arrange: create a user with known balance
        var userResponse = await _api.PostAsync("/api/users", new()
        {
            DataObject = new { name = $"user_{Guid.NewGuid():N}", balance = 100.00m }
        });
        var user = await userResponse.JsonAsync();
        var userId = user.Value.GetProperty("id").GetString();

        // Arrange: create a market with a selection
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
        var selectionId = market.Value.GetProperty("selections")[0].GetProperty("id").GetString();

        // Act: place a bet
        var betResponse = await _api.PostAsync($"/api/users/{userId}/bets", new()
        {
            DataObject = new { selectionId, stake = 10.00m }
        });
        Assert.That(betResponse.Status, Is.EqualTo(202));

        // Assert: poll until bet becomes Active (async processing)
        var betIsActive = await PollUntilAsync(async () =>
        {
            var betsResponse = await _api.GetAsync($"/api/users/{userId}/bets");
            var bets = await betsResponse.JsonAsync();
            var betsArray = bets.Value.EnumerateArray().ToList();
            return betsArray.Count == 1 &&
                   betsArray[0].GetProperty("state").GetString() == "Active";
        }, timeoutMs: 5000);

        Assert.That(betIsActive, Is.True, "Bet should transition to Active state");

        // Assert: balance should be deducted
        var balanceResponse = await _api.GetAsync($"/api/users/{userId}/balance");
        var balance = await balanceResponse.JsonAsync();
        Assert.That(balance.Value.GetProperty("amount").GetDecimal(), Is.EqualTo(90.00m));
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