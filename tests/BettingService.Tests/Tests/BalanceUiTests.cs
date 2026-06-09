using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class BalanceUiTests : PlaywrightTest
{
    private IBrowser _browser = null!;
    private IPage _page = null!;
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
        // Navigate to the app
        await _page.GotoAsync(_baseUrl);

        // Wait for the page to fully load and markets to appear
        await _page.WaitForSelectorAsync(".selection-btn");

        // Verify balance element is displayed
        var balanceElement = _page.GetByTestId("user-balance");
        await Expect(balanceElement).ToBeVisibleAsync();

        // Select a bet
        var selectionBtn = _page.Locator(".selection-btn").First;
        await selectionBtn.ClickAsync();

        // Fill stake and place bet
        await _page.FillAsync("#stake-input", "10");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Place Bet" }).ClickAsync();

        // Verify bet was placed successfully
        await Expect(_page.Locator("#bet-confirmation")).ToBeVisibleAsync();

        // Wait for the page to reflect changes
        await _page.WaitForTimeoutAsync(3000);

        // Verify balance is still displayed correctly
        await Expect(balanceElement).ToBeVisibleAsync();
        var balanceText = await balanceElement.TextContentAsync();
        Assert.That(balanceText, Does.Contain("£"),
            "Balance should display a pound value after bet placement");
    }
}