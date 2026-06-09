# TC-004: Settlement Pays Winning Bet Correctly

## Preconditions
- User created with £100.00 balance
- Market "Match Winner" with selections: Team A (2.50), Team B (1.80)
- Active bet: £10.00 on "Team A" at odds 2.50

## Steps

| # | Action | Data | Expected Result |
|---|--------|------|-----------------|
| 1 | Post event result | Winner: Team A (selection ID) | HTTP 200; message confirms settlement in progress |
| 2 | Wait for settlement to process | — | Bet state transitions to WON |
| 3 | Verify bet settlement details | — | Bet state = "Won"; payout = £25.00 (stake × odds = 10 × 2.50) |
| 4 | Verify balance credited | — | User balance = £115.00 (original £100 − £10 stake + £25 payout) |
