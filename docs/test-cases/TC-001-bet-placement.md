# TC-001: Bet Placement Creates Active Bet and Deducts Balance

## Preconditions
- User created with £100.00 balance
- Market "Match Winner" in state OPEN
- Selection "Team A" priced at 2.50

## Steps

| # | Action | Data | Expected Result |
|---|--------|------|-----------------|
| 1 | Place bet on selection | Stake: £10.00, Selection: "Team A" @ 2.50 | HTTP 202 Accepted returned |
| 2 | Wait for bet to be processed | — | Bet state transitions to ACTIVE |
| 3 | Verify bet details | — | Bet exists with: state = ACTIVE, stake = £10.00, odds = 2.50, selectionName = "Team A" |
| 4 | Verify balance deducted | — | User balance = £90.00 |
