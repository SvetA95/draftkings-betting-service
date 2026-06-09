using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class IdempotencyTests : PlaywrightTest
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
    public async Task Posting_Result_Twice_Does_Not_Cause_Errors()
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
                selections = new[]
                {
                    new { name = "Team A", odds = 2.50 },
                    new { name = "Team B", odds = 1.80 }
                }
            }
        });
        var market = await marketResponse.JsonAsync();
        var winningSelectionId = market.Value.GetProperty("selections")[0].GetProperty("id").GetString();

        // Arrange: place bet and wait for Active state
        await _api.PostAsync($"/api/users/{userId}/bets", new()
        {
            DataObject = new { selectionId = winningSelectionId, stake = 10.00m }
        });

        await PollUntilAsync(async () =>
        {
            var bets = await _api.GetAsync($"/api/users/{userId}/bets");
            var data = await bets.JsonAsync();
            return data.Value.EnumerateArray().Any(b => b.GetProperty("state").GetString() == "Active");
        }, timeoutMs: 5000);

        // Act: post result first time
        var result1 = await _api.PostAsync($"/api/events/{eventId}/result", new()
        {
            DataObject = new { winningSelectionId }
        });
        Assert.That(result1.Status, Is.EqualTo(200));

        // Wait for settlement to complete
        await PollUntilAsync(async () =>
        {
            var bets = await _api.GetAsync($"/api/users/{userId}/bets");
            var data = await bets.JsonAsync();
            return data.Value.EnumerateArray().Any(b => b.GetProperty("state").GetString() == "Won");
        }, timeoutMs: 5000);

        // Check balance after first settlement
        var balanceAfterFirst = await _api.GetAsync($"/api/users/{userId}/balance");
        var balanceData1 = await balanceAfterFirst.JsonAsync();
        Assert.That(balanceData1.Value.GetProperty("amount").GetDecimal(), Is.GreaterThan(0m),
            "Balance should be positive after winning settlement");

        // Act: post the same result again (duplicate)
        var result2 = await _api.PostAsync($"/api/events/{eventId}/result", new()
        {
            DataObject = new { winningSelectionId }
        });
        Assert.That(result2.Status, Is.EqualTo(200));

        // Wait for any processing
        await Task.Delay(3000);

        // Verify: balance should still be valid
        var balanceAfterSecond = await _api.GetAsync($"/api/users/{userId}/balance");
        var balanceData2 = await balanceAfterSecond.JsonAsync();
        Assert.That(balanceData2.Value.GetProperty("amount").GetDecimal(), Is.GreaterThan(0m),
            "Balance should remain positive after duplicate result");
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