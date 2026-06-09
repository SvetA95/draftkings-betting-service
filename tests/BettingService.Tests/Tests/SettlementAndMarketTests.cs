using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

// Tests for settlement correctness and edge cases around market state transitions.
[TestFixture]
public class SettlementAndMarketTests : PlaywrightTest
{
    private IAPIRequestContext _api = null!;
    private readonly string _baseUrl = "http://localhost:5000";

    [SetUp]
    public async Task SetUp()
    {
        _api = await Playwright.APIRequest.NewContextAsync(new() { BaseURL = _baseUrl });
    }

    [TearDown]
    public async Task TearDown()
    {
        await _api.DisposeAsync();
    }

    // Losing bet settles as Lost with no payout
    [Test]
    public async Task Settlement_Marks_Losing_Bet_As_Lost_With_No_Payout()
    {
        // Arrange
        var userId = await BettingApiHelper.CreateUserAsync(_api, balance: 100.00m);
        var market = await BettingApiHelper.CreateMarketAsync(_api,
            ("Team A", 2.50m),
            ("Team B", 1.80m));

        var losingSelectionId = market.SelectionIds[1]; // Team B
        var winningSelectionId = market.SelectionIds[0]; // Team A

        await BettingApiHelper.PlaceBetAsync(_api, userId, losingSelectionId, stake: 10.00m);

        var betActive = await BettingApiHelper.WaitForBetStateAsync(_api, userId, "Active");
        Assert.That(betActive, Is.True, "Bet must be Active before settlement");

        // Act: Team A wins — our bet on Team B loses
        await BettingApiHelper.PostResultAsync(_api, market.EventId, winningSelectionId);

        // Assert: bet settles as Lost
        var settled = await BettingApiHelper.PollUntilAsync(async () =>
        {
            var bets = await BettingApiHelper.GetBetsAsync(_api, userId);
            var bet = bets.FirstOrDefault();
            return bet.GetProperty("state").GetString() == "Lost";
        });
        Assert.That(settled, Is.True, "Losing bet should settle as Lost");

        // Assert: payout is zero — stake already gone, nothing credited back
        var bets = await BettingApiHelper.GetBetsAsync(_api, userId);
        var payout = bets[0].GetProperty("payout").GetDecimal();
        Assert.That(payout, Is.EqualTo(0.00m), "Losing bet should have zero payout");

        // Assert: balance stays at £90 (stake deducted, no return)
        var balance = await BettingApiHelper.GetBalanceAsync(_api, userId);
        Assert.That(balance, Is.EqualTo(90.00m),
            "Balance should remain at £90 — stake deducted, no payout on losing bet");
    }

    // Resumed market accepts new bets
    [Test]
    public async Task Resumed_Market_Accepts_New_Bets()
    {
        // Arrange
        var userId = await BettingApiHelper.CreateUserAsync(_api, balance: 100.00m);
        var market = await BettingApiHelper.CreateMarketAsync(_api, ("Team A", 2.50m));

        // Suspend and wait for confirmation
        await BettingApiHelper.SuspendMarketAsync(_api, market.MarketId);
        var suspended = await BettingApiHelper.WaitForMarketStateAsync(_api, market.MarketId, "Suspended");
        Assert.That(suspended, Is.True, "Market should reach Suspended state");

        // Act: resume
        await BettingApiHelper.ResumeMarketAsync(_api, market.MarketId);
        var resumed = await BettingApiHelper.WaitForMarketStateAsync(_api, market.MarketId, "Open");
        Assert.That(resumed, Is.True, "Market should return to Open state after resume");

        // Assert: bets are accepted again on the resumed market
        var betResponse = await _api.PostAsync($"/api/users/{userId}/bets", new()
        {
            DataObject = new
            {
                selectionId = Guid.Parse(market.SelectionIds[0]),
                stake = 10.00m
            }
        });

        Assert.That(betResponse.Status, Is.EqualTo(202),
            "Resumed market should accept new bets — HTTP 202 expected");

        // Assert: balance deducted confirms the bet was actually processed
        var balance = await BettingApiHelper.GetBalanceAsync(_api, userId);
        Assert.That(balance, Is.EqualTo(90.00m), "Stake should be deducted after placing bet on resumed market");
    }

    // Cashout then settle causes double payout
    [Test]
    public async Task Cashout_Then_Settle_Does_Not_Pay_Out_Twice()
    {
        // Arrange
        var userId = await BettingApiHelper.CreateUserAsync(_api, balance: 100.00m);
        var market = await BettingApiHelper.CreateMarketAsync(_api,
            ("Team A", 2.50m),
            ("Team B", 1.80m));

        var winningSelectionId = market.SelectionIds[0]; // Team A

        // Place bet on the winning selection
        var betId = await BettingApiHelper.PlaceBetAsync(_api, userId, winningSelectionId, stake: 10.00m);

        var betActive = await BettingApiHelper.WaitForBetStateAsync(_api, userId, "Active");
        Assert.That(betActive, Is.True, "Bet must be Active before cashout");

        // Act: user cashes out — stake returned (odds unchanged so cashout = £10)
        // Balance after cashout: £90 + £10 = £100
        await BettingApiHelper.CashoutBetAsync(_api, betId);

        var balanceAfterCashout = await BettingApiHelper.GetBalanceAsync(_api, userId);
        Assert.That(balanceAfterCashout, Is.EqualTo(100.00m),
            "Balance should be restored to £100 after cashout at same odds");

        // Act: result posted — Team A wins
        await BettingApiHelper.PostResultAsync(_api, market.EventId, winningSelectionId);

        // Wait for any async settlement processing
        await Task.Delay(3000);

        // Assert: balance must remain £100 — the cashed-out bet should not be settled again
        // BUG: current implementation pays £25 on top, giving £125
        var balanceAfterSettlement = await BettingApiHelper.GetBalanceAsync(_api, userId);
        Assert.That(balanceAfterSettlement, Is.EqualTo(100.00m),
            "Balance should remain £100 — cashed-out bet must not receive a second payout on settlement. " +
            "KNOWN BUG: ProcessSettlement does not guard against Void bet state.");
    }
}
