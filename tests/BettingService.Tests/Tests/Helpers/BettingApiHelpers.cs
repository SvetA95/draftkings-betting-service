using System.Text.Json;
using Microsoft.Playwright;

namespace BettingService.Tests.Tests;

// Shared helpers to eliminate boilerplate across test fixtures.
// Keeps individual tests focused on behaviour, not setup mechanics.
public static class BettingApiHelper
{
    // Users

    public static async Task<string> CreateUserAsync(IAPIRequestContext api, decimal balance = 100.00m)
    {
        var response = await api.PostAsync("/api/users", new()
        {
            DataObject = new { name = $"user_{Guid.NewGuid():N}", balance }
        });
        var json = await response.JsonAsync();
        return json.Value.GetProperty("id").GetString()!;
    }

    public static async Task<decimal> GetBalanceAsync(IAPIRequestContext api, string userId)
    {
        var response = await api.GetAsync($"/api/users/{userId}/balance");
        var json = await response.JsonAsync();
        return json.Value.GetProperty("amount").GetDecimal();
    }

    // Markets

    public record MarketDetails(string MarketId, string EventId, List<string> SelectionIds);

    // Creates a market with the given selection names and odds.
    public static async Task<MarketDetails> CreateMarketAsync(
        IAPIRequestContext api,
        params (string Name, decimal Odds)[] selections)
    {
        var eventId = Guid.NewGuid().ToString();
        var response = await api.PostAsync("/api/markets", new()
        {
            DataObject = new
            {
                name = "Match Winner",
                eventId = Guid.Parse(eventId),
                eventName = "Test Match",
                selections = selections.Select(s => new { name = s.Name, odds = s.Odds }).ToArray()
            }
        });
        var json = await response.JsonAsync();
        var marketId = json.Value.GetProperty("id").GetString()!;
        var selectionIds = json.Value.GetProperty("selections")
            .EnumerateArray()
            .Select(s => s.GetProperty("id").GetString()!)
            .ToList();

        return new MarketDetails(marketId, eventId, selectionIds);
    }

    // Bets

    public static async Task<string> PlaceBetAsync(
        IAPIRequestContext api,
        string userId,
        string selectionId,
        decimal stake = 10.00m)
    {
        var response = await api.PostAsync($"/api/users/{userId}/bets", new()
        {
            DataObject = new { selectionId = Guid.Parse(selectionId), stake }
        });
        var json = await response.JsonAsync();
        return json.Value.GetProperty("betId").GetString()!;
    }

    public static async Task<List<JsonElement>> GetBetsAsync(IAPIRequestContext api, string userId)
    {
        var response = await api.GetAsync($"/api/users/{userId}/bets");
        var json = await response.JsonAsync();
        return json.Value.EnumerateArray().ToList();
    }

    // Settlement

    public static async Task PostResultAsync(IAPIRequestContext api, string eventId, string winningSelectionId)
    {
        await api.PostAsync($"/api/events/{eventId}/result", new()
        {
            DataObject = new { winningSelectionId = Guid.Parse(winningSelectionId) }
        });
    }

    // Market state

    public static async Task SuspendMarketAsync(IAPIRequestContext api, string marketId)
        => await api.PostAsync($"/api/markets/{marketId}/suspend", new() { });

    public static async Task ResumeMarketAsync(IAPIRequestContext api, string marketId)
        => await api.PostAsync($"/api/markets/{marketId}/resume", new() { });

    public static async Task<string> GetMarketStateAsync(IAPIRequestContext api, string marketId)
    {
        var response = await api.GetAsync($"/api/markets/{marketId}");
        var json = await response.JsonAsync();
        return json.Value.GetProperty("state").GetString()!;
    }

    // Cashout

    public static async Task<decimal> CashoutBetAsync(IAPIRequestContext api, string betId)
    {
        var response = await api.PostAsync($"/api/bets/{betId}/cashout", new() { });
        var json = await response.JsonAsync();
        return json.Value.GetProperty("paidOut").GetDecimal();
    }

    // Polling 

    // Polls a condition every intervalMs until it returns true or timeoutMs elapses.
    public static async Task<bool> PollUntilAsync(
        Func<Task<bool>> condition,
        int timeoutMs = 5000,
        int intervalMs = 250)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return true;
            await Task.Delay(intervalMs);
        }
        return false;
    }

    // waits until the user's most recent bet reaches the expected state.
    public static Task<bool> WaitForBetStateAsync(
        IAPIRequestContext api,
        string userId,
        string expectedState,
        int timeoutMs = 5000)
        => PollUntilAsync(async () =>
        {
            var bets = await GetBetsAsync(api, userId);
            return bets.Any(b => b.GetProperty("state").GetString() == expectedState);
        }, timeoutMs);

    // waits until the market reaches the expected state.
    public static Task<bool> WaitForMarketStateAsync(
        IAPIRequestContext api,
        string marketId,
        string expectedState,
        int timeoutMs = 5000)
        => PollUntilAsync(async () =>
            await GetMarketStateAsync(api, marketId) == expectedState,
            timeoutMs);
}
