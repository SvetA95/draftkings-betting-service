namespace BettingService.Tests.Tests;

[TestFixture]
public class BetSurvivesSuspensionTests : PlaywrightTest
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
    public async Task Active_Bet_Survives_Suspension_And_Settles_Correctly_After_Resume()
    {
        // Arrange: create user and market
        var userId = await BettingApiHelper.CreateUserAsync(_api, balance: 100.00m);
        var market = await BettingApiHelper.CreateMarketAsync(_api, ("Team A", 2.50m));

        // Arrange: place bet and wait for Active state
        await BettingApiHelper.PlaceBetAsync(_api, userId, market.SelectionIds[0], stake: 10.00m);

        var betActive = await BettingApiHelper.WaitForBetStateAsync(_api, userId, "Active");
        Assert.That(betActive, Is.True, "Bet must be Active before suspension");

        // Act: suspend the market
        await BettingApiHelper.SuspendMarketAsync(_api, market.MarketId);
        var suspended = await BettingApiHelper.WaitForMarketStateAsync(_api, market.MarketId, "Suspended");
        Assert.That(suspended, Is.True, "Market should reach Suspended state");

        // Assert: bet state must still be Active — suspension must not affect existing bets
        var betsAfterSuspension = await BettingApiHelper.GetBetsAsync(_api, userId);
        Assert.That(betsAfterSuspension, Is.Not.Empty, "Bet should still exist after market suspension");
        Assert.That(betsAfterSuspension[0].GetProperty("state").GetString(), Is.EqualTo("Active"),
            "Bet state must remain Active after market suspension — suspension should not void existing bets");

        // Act: resume the market
        await BettingApiHelper.ResumeMarketAsync(_api, market.MarketId);
        var resumed = await BettingApiHelper.WaitForMarketStateAsync(_api, market.MarketId, "Open");
        Assert.That(resumed, Is.True, "Market should return to Open state after resume");

        // Act: settle the event — Team A wins
        await BettingApiHelper.PostResultAsync(_api, market.EventId, market.SelectionIds[0]);

        // Assert: bet settles correctly despite the suspend/resume cycle
        var settled = await BettingApiHelper.PollUntilAsync(async () =>
        {
            var bets = await BettingApiHelper.GetBetsAsync(_api, userId);
            return bets.Any(b => b.GetProperty("state").GetString() == "Won");
        });
        Assert.That(settled, Is.True, "Bet should settle as Won after resume and result");

        var finalBets = await BettingApiHelper.GetBetsAsync(_api, userId);
        Assert.That(finalBets[0].GetProperty("payout").GetDecimal(), Is.EqualTo(25.00m),
            "Payout should be stake × odds = £25");

        // Assert: balance = £100 − £10 stake + £25 payout = £115
        var balance = await BettingApiHelper.GetBalanceAsync(_api, userId);
        Assert.That(balance, Is.EqualTo(115.00m),
            "Balance should be £115 after winning settlement following suspend/resume cycle");
    }
}