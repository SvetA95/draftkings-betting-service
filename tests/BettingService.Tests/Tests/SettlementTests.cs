using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class SettlementTests : PlaywrightTest
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
    public async Task Settlement_Pays_Winning_Bet_Correctly()
    {
        // Arrange: create user
        var userResponse = await _api.PostAsync("/api/users", new()
        {
            DataObject = new { name = $"user_{Guid.NewGuid():N}", balance = 100.00m }
        });
        var user = await userResponse.JsonAsync();
        var userId = user.Value.GetProperty("id").GetString();

        // Arrange: create market with known odds
        var eventId = Guid.NewGuid();
        var marketResponse = await _api.PostAsync("/api/markets", new()
        {
            DataObject = new
            {
                name = "Match Winner",
                eventId,
                eventName = "Test Match",
                selections = new[]
                {
                    new { name = "Team A", odds = 2.50 },
                    new { name = "Team B", odds = 1.80 }
                }
            }
        });
        var market = await marketResponse.JsonAsync();
        var winningSelectionId = market.Value.GetProperty("selections")[0].GetProperty("id").GetString();

        // Arrange: place bet and wait for it to become Active
        await _api.PostAsync($"/api/users/{userId}/bets", new()
        {
            DataObject = new { selectionId = winningSelectionId, stake = 10.00m }
        });

        var betActive = await PollUntilAsync(async () =>
        {
            var bets = await _api.GetAsync($"/api/users/{userId}/bets");
            var data = await bets.JsonAsync();
            return data.Value.EnumerateArray().Any(b => b.GetProperty("state").GetString() == "Active");
        }, timeoutMs: 5000);
        Assert.That(betActive, Is.True, "Bet must be Active before settlement");

        // Act: post result — Team A wins
        var resultResponse = await _api.PostAsync($"/api/events/{eventId}/result", new()
        {
            DataObject = new { winningSelectionId }
        });
        Assert.That(resultResponse.Status, Is.EqualTo(200));

        // Assert: poll until bet is settled as Won with correct payout
        var settled = await PollUntilAsync(async () =>
        {
            var bets = await _api.GetAsync($"/api/users/{userId}/bets");
            var data = await bets.JsonAsync();
            var bet = data.Value.EnumerateArray().FirstOrDefault();
            return bet.GetProperty("state").GetString() == "Won" &&
                   bet.GetProperty("payout").GetDecimal() == 25.00m;
        }, timeoutMs: 5000);

        Assert.That(settled, Is.True, "Bet should settle as Won with payout = stake × odds");

        // Assert: balance = original - stake + payout = 100 - 10 + 25 = 115
        var balanceResponse = await _api.GetAsync($"/api/users/{userId}/balance");
        var balance = await balanceResponse.JsonAsync();
        Assert.That(balance.Value.GetProperty("amount").GetDecimal(), Is.EqualTo(115.00m));
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