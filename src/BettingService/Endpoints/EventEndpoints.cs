using BettingService.Data;
using BettingService.Models;
using BettingService.Services;
using Microsoft.EntityFrameworkCore;

namespace BettingService.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/events");

        group.MapGet("/{eventId}", async (Guid eventId, BettingDbContext db) =>
        {
            var evt = await db.Events
                .Include(e => e.Markets)
                .ThenInclude(m => m.Selections)
                .FirstOrDefaultAsync(e => e.Id == eventId);
            if (evt is null) return Results.NotFound();

            return Results.Ok(new EventResponse(
                evt.Id, evt.Name, evt.State.ToString(), evt.WinningSelectionId,
                evt.Markets.Select(m => new MarketResponse(
                    m.Id, m.Name, m.State.ToString(), m.EventId,
                    m.Selections.Select(s => new SelectionResponse(s.Id, s.Name, s.Odds)).ToList()
                )).ToList()));
        });

        group.MapPost("/{eventId}/result", async (
            Guid eventId,
            PostResultRequest request,
            BettingDbContext db,
            AsyncProcessingService processor) =>
        {
            var evt = await db.Events.FindAsync(eventId);
            if (evt is null) return Results.NotFound(new { error = "Event not found" });

            // BUG: No idempotency check — posting result twice will settle (and pay) again
            evt.State = EventState.Resulted;
            await db.SaveChangesAsync();

            processor.Enqueue(new SettlementJob(eventId, request.WinningSelectionId));

            return Results.Ok(new { message = "Result accepted, settlement in progress" });
        });
    }
}

public record PostResultRequest(Guid WinningSelectionId);
public record EventResponse(Guid Id, string Name, string State, Guid? WinningSelectionId, List<MarketResponse> Markets);
