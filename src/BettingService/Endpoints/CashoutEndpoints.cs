using BettingService.Data;
using BettingService.Models;

namespace BettingService.Endpoints;

public static class CashoutEndpoints
{
    public static void MapCashoutEndpoints(this WebApplication app)
    {
        app.MapGet("/api/bets/{betId}/cashout-offer", async (Guid betId, BettingDbContext db) =>
        {
            var bet = await db.Bets.FindAsync(betId);
            if (bet is null) return Results.NotFound();
            if (bet.State != BetState.Active)
                return Results.BadRequest(new { error = "Bet is not active" });

            var selection = await db.Selections.FindAsync(bet.SelectionId);
            if (selection is null) return Results.NotFound();

            // Cashout value based on current odds (which may have drifted from placement odds)
            var cashoutValue = bet.Stake * (selection.Odds / bet.Odds);
            return Results.Ok(new CashoutOfferResponse(betId, Math.Round(cashoutValue, 2), DateTime.UtcNow));
        });

        app.MapPost("/api/bets/{betId}/cashout", async (Guid betId, BettingDbContext db) =>
        {
            var bet = await db.Bets.FindAsync(betId);
            if (bet is null) return Results.NotFound();
            if (bet.State != BetState.Active)
                return Results.BadRequest(new { error = "Bet is not active" });

            var selection = await db.Selections.FindAsync(bet.SelectionId);
            if (selection is null) return Results.NotFound();

            var cashoutValue = Math.Round(bet.Stake * (selection.Odds / bet.Odds), 2);

            var user = await db.Users.FindAsync(bet.UserId);
            if (user is null) return Results.NotFound();

            bet.State = BetState.Void;
            bet.Payout = cashoutValue;
            bet.SettledAt = DateTime.UtcNow;
            user.Balance += cashoutValue;

            await db.SaveChangesAsync();

            return Results.Ok(new CashoutResultResponse(betId, cashoutValue, user.Balance));
        });

        // Allows changing odds on a selection (simulates price drift)
        app.MapPut("/api/selections/{selectionId}/odds", async (
            Guid selectionId,
            UpdateOddsRequest request,
            BettingDbContext db) =>
        {
            var selection = await db.Selections.FindAsync(selectionId);
            if (selection is null) return Results.NotFound();

            selection.Odds = request.NewOdds;
            await db.SaveChangesAsync();

            return Results.Ok(new { selectionId, odds = selection.Odds });
        });
    }
}

public record CashoutOfferResponse(Guid BetId, decimal OfferedAmount, DateTime CalculatedAt);
public record CashoutResultResponse(Guid BetId, decimal PaidOut, decimal NewBalance);
public record UpdateOddsRequest(decimal NewOdds);
