# TC-003: Bet Slip Displays Selections with Correct Odds

## Preconditions
- Market "Match Winner" in state OPEN
- Selections available: Team A (2.50), Team B (1.80), Draw (3.20)

## Steps

| # | Action | Data | Expected Result |
|---|--------|------|-----------------|
| 1 | Navigate to the application | URL: http://localhost:5000 | Page loads; market section displays all selections with their names and odds |
| 2 | Verify selection details displayed | — | Each selection shows name and odds value matching the market data |
| 3 | Click a selection's bet button | Selection: "Team A" @ 2.50 | Bet slip section populates with selection name "Team A" and odds "2.50" |
| 4 | Enter stake amount | Stake: £10.00 | Potential payout displays correctly: £25.00 (stake × odds) |
