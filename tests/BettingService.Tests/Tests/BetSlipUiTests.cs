using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class BetSlipUiTests : PlaywrightTest
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
    public async Task Bet_Slip_Displays_Selections_With_Correct_Odds()
    {
        // Navigate to the page
        await _page.GotoAsync(_baseUrl);

        // Wait for markets to load
        await _page.WaitForSelectorAsync(".selection-btn");

        // Assert: selections are visible with correct odds from the UI
        var selections = await _page.QuerySelectorAllAsync(".selection-btn");
        Assert.That(selections, Has.Count.GreaterThanOrEqualTo(1));

        // Click the first selection
        var firstSelection = selections[0];
        var selectionName = await firstSelection.QuerySelectorAsync(".selection-name");
        var selectionOdds = await firstSelection.QuerySelectorAsync(".selection-odds");

        var nameText = await selectionName!.TextContentAsync();
        var oddsText = await selectionOdds!.TextContentAsync();

        Assert.That(nameText, Is.Not.Null.And.Not.Empty);
        Assert.That(decimal.Parse(oddsText!), Is.GreaterThan(1.0m));

        // Click the selection button
        await firstSelection.ClickAsync();

        // Assert: bet slip populates with selection details
        var betSlipName = await _page.TextContentAsync("#betslip-selection-name");
        var betSlipOdds = await _page.TextContentAsync("#betslip-odds");

        Assert.That(betSlipName, Is.EqualTo(nameText));
        Assert.That(betSlipOdds, Is.EqualTo(oddsText));

        // Enter stake and verify payout calculation
        await _page.FillAsync("#stake-input", "10");
        var expectedPayout = $"£{(10m * decimal.Parse(oddsText!)):F2}";
        var displayedPayout = await _page.TextContentAsync("#potential-payout");
        Assert.That(displayedPayout, Is.EqualTo(expectedPayout));
    }
}