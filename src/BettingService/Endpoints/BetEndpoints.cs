using BettingService.Data;
using BettingService.Models;
using BettingService.Services;

namespace BettingService.Endpoints;

public static class BetEndpoints
{
    public static void MapBetEndpoints(this WebApplication app)
    {
        app.MapPost("/api/users/{userId}/bets", async (
            Guid userId,
            PlaceBetRequest request,
            BettingDbContext db,
            AsyncProcessingService processor) =>
        {
            var user = await db.Users.FindAsync(userId);
            if (user is null) return Results.NotFound(new { error = "User not found" });

            var selection = await db.Selections.FindAsync(request.SelectionId);
            if (selection is null) return Results.NotFound(new { error = "Selection not found" });

            var market = await db.Markets.FindAsync(selection.MarketId);
            if (market is null) return Results.NotFound(new { error = "Market not found" });

            if (market.State == MarketState.Suspended)
                return Results.Conflict(new { error = "Market is suspended" });

            if (market.State == MarketState.Closed)
                return Results.Conflict(new { error = "Market is closed" });

            if (user.Balance < request.Stake)
                return Results.BadRequest(new { error = "Insufficient balance" });

            if (request.Stake <= 0)
                return Results.BadRequest(new { error = "Stake must be positive" });

            user.Balance -= request.Stake;

            var bet = new Bet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MarketId = market.Id,
                SelectionId = selection.Id,
                SelectionName = selection.Name,
                Stake = request.Stake,
                Odds = selection.Odds,
                State = BetState.Pending,
                PlacedAt = DateTime.UtcNow
            };

            db.Bets.Add(bet);
            await db.SaveChangesAsync();

            processor.Enqueue(new BetActivationJob(bet.Id));

            return Results.Accepted($"/api/users/{userId}/bets",
                new BetPlacedResponse(bet.Id, bet.State.ToString()));
        });
    }
}

public record PlaceBetRequest(Guid SelectionId, decimal Stake);
public record BetPlacedResponse(Guid BetId, string State);
