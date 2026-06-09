# TC-005: Active Bet Survives Market Suspension

## Preconditions
- User created with £100.00 balance
- Market "Match Winner" in state OPEN
- Active bet: £10.00 on "Team A" at odds 2.50 (state = ACTIVE)

## Steps

| # | Action | Data | Expected Result |
|---|--------|------|-----------------|
| 1 | Verify bet is in ACTIVE state | — | Bet state = "Active" |
| 2 | Suspend market | Market: "Match Winner" | Market state transitions to SUSPENDED |
| 3 | Verify bet state after suspension | — | Bet state remains "Active" (not Void, not any other state) |
| 4 | Resume market | Market: "Match Winner" | Market state transitions back to OPEN |
| 5 | Post event result: Team A wins | Winner: Team A | Settlement processes normally |
| 6 | Verify bet settles correctly | — | Bet state = "Won"; payout = £25.00; balance = £115.00 |
