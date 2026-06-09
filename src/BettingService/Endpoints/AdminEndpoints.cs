using BettingService.Data;

namespace BettingService.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapPost("/api/admin/reset", async (BettingDbContext db) =>
        {
            db.Bets.RemoveRange(db.Bets);
            db.Selections.RemoveRange(db.Selections);
            db.Markets.RemoveRange(db.Markets);
            db.Events.RemoveRange(db.Events);
            db.Users.RemoveRange(db.Users);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "All data reset" });
        });
    }
}
