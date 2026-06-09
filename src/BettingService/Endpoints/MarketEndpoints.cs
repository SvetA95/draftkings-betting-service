using BettingService.Data;
using BettingService.Models;
using BettingService.Services;
using Microsoft.EntityFrameworkCore;

namespace BettingService.Endpoints;

public static class MarketEndpoints
{
    public static void MapMarketEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/markets");

        group.MapPost("/", async (CreateMarketRequest request, BettingDbContext db) =>
        {
            var evt = await db.Events.FindAsync(request.EventId);
            if (evt is null)
            {
                evt = new SportEvent
                {
                    Id = request.EventId,
                    Name = request.EventName ?? "Event",
                    State = EventState.Live
                };
                db.Events.Add(evt);
            }

            var market = new Market
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                EventId = request.EventId,
                State = MarketState.Open,
                Selections = request.Selections.Select(s => new Selection
                {
                    Id = Guid.NewGuid(),
                    Name = s.Name,
                    Odds = s.Odds
                }).ToList()
            };

            db.Markets.Add(market);
            await db.SaveChangesAsync();

            return Results.Created($"/api/markets/{market.Id}", new MarketResponse(
                market.Id, market.Name, market.State.ToString(), market.EventId,
                market.Selections.Select(s => new SelectionResponse(s.Id, s.Name, s.Odds)).ToList()));
        });

        group.MapGet("/{marketId}", async (Guid marketId, BettingDbContext db) =>
        {
            var market = await db.Markets
                .Include(m => m.Selections)
                .FirstOrDefaultAsync(m => m.Id == marketId);
            if (market is null) return Results.NotFound();

            return Results.Ok(new MarketResponse(
                market.Id, market.Name, market.State.ToString(), market.EventId,
                market.Selections.Select(s => new SelectionResponse(s.Id, s.Name, s.Odds)).ToList()));
        });

        group.MapPost("/{marketId}/suspend", async (
            Guid marketId,
            BettingDbContext db,
            AsyncProcessingService processor) =>
        {
            var market = await db.Markets.FindAsync(marketId);
            if (market is null) return Results.NotFound();
            if (market.State == MarketState.Suspended)
                return Results.Ok(new { message = "Already suspended" });

            processor.Enqueue(new MarketSuspensionJob(marketId));
            return Results.Ok(new { message = "Suspension initiated" });
        });

        group.MapPost("/{marketId}/resume", async (
            Guid marketId,
            BettingDbContext db,
            AsyncProcessingService processor) =>
        {
            var market = await db.Markets.FindAsync(marketId);
            if (market is null) return Results.NotFound();
            if (market.State != MarketState.Suspended)
                return Results.Ok(new { message = "Not suspended" });

            processor.Enqueue(new MarketResumeJob(marketId));
            return Results.Ok(new { message = "Resume initiated" });
        });
    }
}

public record CreateMarketRequest(string Name, Guid EventId, string? EventName, List<CreateSelectionRequest> Selections);
public record CreateSelectionRequest(string Name, decimal Odds);
public record MarketResponse(Guid Id, string Name, string State, Guid EventId, List<SelectionResponse> Selections);
public record SelectionResponse(Guid Id, string Name, decimal Odds);
