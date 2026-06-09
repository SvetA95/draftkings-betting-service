using BettingService.Data;
using BettingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BettingService.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users");

        group.MapPost("/", async (CreateUserRequest request, BettingDbContext db) =>
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Balance = request.Balance
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Created($"/api/users/{user.Id}", new UserResponse(user.Id, user.Name, user.Balance));
        });

        group.MapGet("/{userId}/balance", async (Guid userId, BettingDbContext db) =>
        {
            var user = await db.Users.FindAsync(userId);
            if (user is null) return Results.NotFound();
            return Results.Ok(new BalanceResponse(user.Balance));
        });

        group.MapGet("/{userId}/bets", async (Guid userId, BettingDbContext db) =>
        {
            var bets = await db.Bets
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.PlacedAt)
                .Select(b => new BetResponse(
                    b.Id, b.SelectionName, b.Odds, b.Stake,
                    b.State.ToString(), b.Payout, b.PlacedAt, b.SettledAt))
                .ToListAsync();
            return Results.Ok(bets);
        });
    }
}

public record CreateUserRequest(string Name, decimal Balance);
public record UserResponse(Guid Id, string Name, decimal Balance);
public record BalanceResponse(decimal Amount);
public record BetResponse(Guid Id, string SelectionName, decimal Odds, decimal Stake,
    string State, decimal? Payout, DateTime PlacedAt, DateTime? SettledAt);
