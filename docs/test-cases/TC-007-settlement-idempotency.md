# TC-007: Settlement Is Idempotent (Duplicate Result)

## Preconditions
- User created with £100.00 balance
- Market "Match Winner" with selections: Team A (2.50), Team B (1.80)
- Active bet: £10.00 on "Team A" at odds 2.50

## Steps

| # | Action | Data | Expected Result |
|---|--------|------|-----------------|
| 1 | Post event result first time | Winner: Team A | Bet settles as WON; payout = £25.00; balance = £115.00 |
| 2 | Verify settlement complete | — | Bet state = "Won"; balance = £115.00 |
| 3 | Post the same event result again (duplicate) | Winner: Team A (same payload) | No error; system handles gracefully |
| 4 | Verify no duplicate payout | — | Balance remains £115.00 (not £140.00); bet still shows single payout of £25.00 |
