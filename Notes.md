# QA Notes

## Test Coverage

### Existing Tests — Changes Made

**`IdempotencyTests.cs` (TC-007)**
The existing assertions were too weak to catch the idempotency bug. The test asserted only that balance was greater than zero, meaning a double payout of £140 would still pass. Assertions updated to check the exact expected balance of £115.00 after first settlement, and £115.00 again after the duplicate result — this test now correctly fails against the current implementation, documenting Bug 1.

**`BetSurvivesSuspensionTests.cs` (TC-005)**
The existing test only checked that the bet still existed after market suspension. It did not assert the bet remained in `Active` state, and never completed the scenario by resuming the market and settling. Test updated to assert `state == "Active"` after suspension, and extended to resume the market and verify correct settlement.

**`BalanceUiTests.cs` (TC-006)**
The assertion only checked that the balance element contained the `£` symbol, which would pass even if the balance never changed. Updated to assert the specific transition from `£100.00` to `£90.00` after placing a £10 bet, matching the spec.

---

### New Tests Added

**`SettlementAndMarketTests.cs`** covers three scenarios not present in the original suite:

**TC-008: Losing Bet Settles as Lost With No Payout**
The original suite only ever settled the winning selection, meaning a bug that paid out all bets regardless of outcome would pass every existing test. This test places a bet on the losing selection, settles the event, and asserts `state == Lost`, `payout == £0`, and `balance == £90`. It is the necessary complement to TC-004.

**TC-009: Resumed Market Accepts New Bets**
TC-002 tests that a suspended market rejects bets, but nothing tested the return path. A market stuck in `Suspended` after a resume call would silently block all betting. This test exercises the full `Open → Suspended → Open` cycle and confirms a bet placed after resume receives HTTP 202 and the stake is deducted.

**TC-010: Cashout Then Settle Does Not Pay Out Twice**
Documents Bug 2 as a failing test. A user cashes out an active bet, then the event result is posted. The test asserts balance remains at the post-cashout value and the settled bet is not paid out again. This test is expected to fail against the current implementation until Bug 2 is fixed.

---

## Shared Test Infrastructure

`BettingApiHelper.cs` was introduced to eliminate the significant boilerplate duplicated across all test fixtures (user creation, market creation, polling, balance checks). All new tests use the helper. Existing tests can be migrated incrementally.

**`Pages/BettingPage.cs`** is a Page Object Model for the main betting UI. All Playwright locators (balance display, selection buttons, stake input, bet slip, confirmation) are centralised here rather than scattered as raw CSS selectors across test files. UI tests reference named properties (`_bettingPage.Balance`, `_bettingPage.PlaceBetAsync(...)`) rather than implementation details. This means if a selector changes in the frontend, only `BettingPage.cs` needs updating rather than every test that touches the UI.

`BalanceUiTests.cs` has been updated to use the POM. `BetSlipUiTests.cs` shares the same locators and should be migrated to use it as well.

---
## Configuration Issues
 
### Dockerfile Targets .NET 8 but Projects Target .NET 10
 
**Location:** `Dockerfile`
 
**Description:**
Both `src/BettingService/BettingService.csproj` and `tests/BettingService.Tests/BettingService.Tests.csproj` target `net10.0`, but the Dockerfile uses the .NET 8 SDK and runtime base images:
 
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
FROM mcr.microsoft.com/dotnet/aspnet:8.0
```
 
This causes the Docker build to fail immediately with `NETSDK1045: The current .NET SDK does not support targeting .NET 10.0`.
 
**Fix:** Update both base images in the Dockerfile to .NET 10:
 
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
FROM mcr.microsoft.com/dotnet/aspnet:10.0
```
 
**Note:** This appears to be an oversight in the provided repository rather than intentional — the project files and Dockerfile were not kept in sync. The fix is a two-line change and was applied locally to get the service running for this assessment.

---

## QA process taken:
Compared the test cases to their matching playwright tests, looked for missing test coverage.

| Spec | Test file | Coverage quality |
| :------- | :------- | :-------- |
TC-001 Bet placement | BetPlacementTests.cs | Good — checks 202, Active state, balance deduction  
TC-002 Suspended market rejects | MarketSuspensionTests.cs | Good — checks 409, balance unchanged 
TC-003 Bet slip UI | BetSlipUiTests.cs | Partial — the test never verifies that all selections are displayed, only that at least one exists (Has.Count.GreaterThanOrEqualTo(1))
TC-004 Settlement pays | SettlementTests.cs | Good — checks Won state, payout, balance
TC-005 Bet survives suspension | BetSurvivesSuspensionTests.cs | Weak — only checks bet still exists, doesn't verify state=Active or that settlement still works after resume
TC-006 Balance UI | BalanceUiTests.cs | Weak — only checks £ symbol present, not actual value change
TC-007 Idempotency | IdempotencyTests.cs | Weak — only checks balance > 0, not that it equals exactly £115
