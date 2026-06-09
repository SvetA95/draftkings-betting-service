using Microsoft.Playwright.NUnit;
using BettingService.Tests.Pages;
using Microsoft.Playwright;

namespace BettingService.Tests.Tests;

[TestFixture]
public class BetSlipUiTests : PlaywrightTest
{
    private IBrowser _browser = null!;
    private IPage _page = null!;
    private BettingPage _bettingPage = null!;
    private readonly string _baseUrl = "http://localhost:5000";

    private static readonly (string Name, decimal Odds)[] ExpectedSelections =
    [
        ("Team A", 2.50m),
        ("Team B", 1.80m),
        ("Draw",   3.20m)
    ];

    [SetUp]
    public async Task SetUp()
    {
        _browser = await Playwright.Chromium.LaunchAsync(new() { Headless = true });
        _page = await _browser.NewPageAsync();
        _bettingPage = new BettingPage(_page);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _page.CloseAsync();
        await _browser.CloseAsync();
    }

    // TC-003: Bet slip displays selections with correct odds
    [Test]
    public async Task Bet_Slip_Displays_Selections_With_Correct_Odds()
    {
        await _bettingPage.GoToAsync(_baseUrl);

        // Step 1-2: all selections are visible with correct names and odds
        Assert.That(await _bettingPage.GetSelectionCountAsync(), Is.EqualTo(ExpectedSelections.Length));

        foreach (var (index, (expectedName, expectedOdds)) in ExpectedSelections.Index())
        {
            var btn = _bettingPage.SelectionButtons.Nth(index);
            Assert.That(await _bettingPage.GetSelectionNameTextAsync(btn), Is.EqualTo(expectedName));
            Assert.That(await _bettingPage.GetSelectionOddsValueAsync(btn), Is.EqualTo(expectedOdds));
        }

        // Step 3: clicking a selection populates the bet slip correctly
        var firstBtn = _bettingPage.SelectionButtons.First;
        await firstBtn.ClickAsync();

        Assert.That(
            (await _bettingPage.BetSlipSelectionName.TextContentAsync())!.Trim(),
            Is.EqualTo(ExpectedSelections[0].Name));
        Assert.That(
            (await _bettingPage.BetSlipOdds.TextContentAsync())!.Trim(),
            Is.EqualTo(ExpectedSelections[0].Odds.ToString("F2")));

        // Step 4: entering a stake shows the correct potential payout (stake × odds)
        await _bettingPage.StakeInput.FillAsync("10");

        Assert.That(
            (await _bettingPage.PotentialPayout.TextContentAsync())!.Trim(),
            Is.EqualTo($"£{10m * ExpectedSelections[0].Odds:F2}"));
    }
}