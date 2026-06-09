using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using BettingService.Tests.Pages;

namespace BettingService.Tests.Tests;

[TestFixture]
public class BalanceUiTests : PlaywrightTest
{
    private IBrowser _browser = null!;
    private IPage _page = null!;
    private BettingPage _bettingPage = null!;
    private IAPIRequestContext _api = null!;
    private readonly string _baseUrl = "http://localhost:5000";

    [SetUp]
    public async Task SetUp()
    {
        _api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl
        });

        _browser = await Playwright.Chromium.LaunchAsync(new() { Headless = true });
        _page = await _browser.NewPageAsync();
        _bettingPage = new BettingPage(_page);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _page.CloseAsync();
        await _browser.CloseAsync();
        await _api.DisposeAsync();
    }

    [Test]
    public async Task Balance_Updates_In_UI_After_Bet_Is_Placed()
    {
        await _bettingPage.GoToAsync(_baseUrl);

        await Expect(_bettingPage.Balance).ToBeVisibleAsync();
        var balanceBefore = await _bettingPage.GetBalanceAsync();

        await _bettingPage.PlaceBetAsync(stake: 10.00m);

        await Expect(_bettingPage.BetConfirmation).ToBeVisibleAsync();

        // Poll for balance update — bet processing is async so UI may lag behind confirmation
        var balanceUpdated = await PollUntilAsync(async () =>
            await _bettingPage.GetBalanceAsync() == balanceBefore - 10.00m);

        Assert.That(balanceUpdated, Is.True,
            $"Balance should decrease by £10. Expected £{balanceBefore - 10.00m:F2} but UI did not update within timeout.");

        var balanceAfter = await _bettingPage.GetBalanceAsync();
        Assert.That(balanceAfter, Is.EqualTo(balanceBefore - 10.00m),
            $"UI balance should be £{balanceBefore - 10.00m:F2} after £10 bet (was £{balanceBefore:F2})");
    }

    private static async Task<bool> PollUntilAsync(Func<Task<bool>> condition, int timeoutMs = 5000, int intervalMs = 250)
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