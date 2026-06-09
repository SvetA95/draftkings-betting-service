# TC-002: Suspended Market Rejects New Bets

## Preconditions
- User created with £100.00 balance
- Market "Match Winner" in state OPEN with selections available

## Steps

| # | Action | Data | Expected Result |
|---|--------|------|-----------------|
| 1 | Suspend market | Market: "Match Winner" | Market state transitions to SUSPENDED |
| 2 | Verify market is suspended | — | GET market returns state = "Suspended" |
| 3 | Attempt to place bet on suspended market | Stake: £10.00 | HTTP 409 Conflict returned; error message indicates market is suspended |
| 4 | Verify no bet was created | — | User's bet list does not contain a new bet |
| 5 | Verify balance unchanged | — | User balance = £100.00 |
