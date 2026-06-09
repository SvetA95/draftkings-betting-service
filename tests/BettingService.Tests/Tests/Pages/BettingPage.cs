using Microsoft.Playwright;

namespace BettingService.Tests.Pages;


public class BettingPage(IPage page)
{
    // Balance
    public ILocator Balance => page.GetByTestId("user-balance");

    // Market / selections
    public ILocator SelectionButtons => page.Locator(".selection-btn");
    public ILocator FirstSelectionButton => SelectionButtons.First;

    public ILocator SelectionName(ILocator selectionButton) => selectionButton.Locator(".selection-name");
    public ILocator SelectionOdds(ILocator selectionButton) => selectionButton.Locator(".selection-odds");

    public async Task<int> GetSelectionCountAsync() => await SelectionButtons.CountAsync();

    public async Task<string> GetSelectionNameTextAsync(ILocator selectionButton)
        => (await SelectionName(selectionButton).TextContentAsync())!.Trim();

    public async Task<decimal> GetSelectionOddsValueAsync(ILocator selectionButton)
        => decimal.Parse((await SelectionOdds(selectionButton).TextContentAsync())!.Trim());

    // Bet slip
    public ILocator StakeInput => page.Locator("#stake-input");
    public ILocator PlaceBetButton => page.GetByRole(AriaRole.Button, new() { Name = "Place Bet" });
    public ILocator BetConfirmation => page.Locator("#bet-confirmation");
    public ILocator BetSlipSelectionName => page.Locator("#betslip-selection-name");
    public ILocator BetSlipOdds => page.Locator("#betslip-odds");
    public ILocator PotentialPayout => page.Locator("#potential-payout");

    // Actions

    public async Task GoToAsync(string baseUrl)
    {
        await page.GotoAsync(baseUrl);
        await page.WaitForSelectorAsync(".selection-btn");
    }

    public async Task SelectAndStakeAsync(decimal stake, ILocator? selectionButton = null)
    {
        await (selectionButton ?? FirstSelectionButton).ClickAsync();
        await StakeInput.FillAsync(stake.ToString("F2"));
    }

    public async Task PlaceBetAsync(decimal stake, ILocator? selectionButton = null)
    {
        await SelectAndStakeAsync(stake, selectionButton);
        await PlaceBetButton.ClickAsync();
    }

    public async Task<decimal> GetBalanceAsync()
    {
        var text = await Balance.TextContentAsync();
        return decimal.Parse(text!.Replace("£", "").Trim());
    }
}