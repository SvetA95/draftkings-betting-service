# TC-006: Balance Updates in UI After Bet Placement

## Preconditions
- User with £100.00 balance displayed on the page
- Market OPEN with at least one selection available

## Steps

| # | Action | Data | Expected Result |
|---|--------|------|-----------------|
| 1 | Navigate to the application | — | Balance displays "£100.00" |
| 2 | Select a bet and enter stake | Stake: £10.00 | Bet slip shows selection and stake |
| 3 | Click "Place Bet" | — | Bet confirmation appears |
| 4 | Wait for balance to update | — | Balance display changes from "£100.00" to "£90.00" |
