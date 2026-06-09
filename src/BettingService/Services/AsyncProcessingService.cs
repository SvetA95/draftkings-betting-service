using System.Threading.Channels;
using BettingService.Data;
using BettingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BettingService.Services;

public record BetActivationJob(Guid BetId);
public record MarketSuspensionJob(Guid MarketId);
public record MarketResumeJob(Guid MarketId);
public record SettlementJob(Guid EventId, Guid WinningSelectionId);

public class AsyncProcessingService : BackgroundService
{
    private readonly Channel<object> _channel = Channel.CreateUnbounded<object>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Random _random = new();

    public AsyncProcessingService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Enqueue(object job) => _channel.Writer.TryWrite(job);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            var delayMs = _random.Next(200, 2001);
            await Task.Delay(delayMs, stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BettingDbContext>();

            switch (job)
            {
                case BetActivationJob activation:
                    await ProcessBetActivation(db, activation);
                    break;
                case MarketSuspensionJob suspension:
                    await ProcessMarketSuspension(db, suspension);
                    break;
                case MarketResumeJob resume:
                    await ProcessMarketResume(db, resume);
                    break;
                case SettlementJob settlement:
                    await ProcessSettlement(db, settlement);
                    break;
            }
        }
    }

    private async Task ProcessBetActivation(BettingDbContext db, BetActivationJob job)
    {
        var bet = await db.Bets.FindAsync(job.BetId);
        if (bet is { State: BetState.Pending })
        {
            bet.State = BetState.Active;
            await db.SaveChangesAsync();
        }
    }

    private async Task ProcessMarketSuspension(BettingDbContext db, MarketSuspensionJob job)
    {
        var market = await db.Markets.FindAsync(job.MarketId);
        if (market is not null)
        {
            market.State = MarketState.Suspended;
            await db.SaveChangesAsync();
        }
    }

    private async Task ProcessMarketResume(BettingDbContext db, MarketResumeJob job)
    {
        var market = await db.Markets.FindAsync(job.MarketId);
        if (market is { State: MarketState.Suspended })
        {
            market.State = MarketState.Open;
            await db.SaveChangesAsync();
        }
    }

    private async Task ProcessSettlement(BettingDbContext db, SettlementJob job)
    {
        var evt = await db.Events.FindAsync(job.EventId);
        if (evt is null) return;

        evt.State = EventState.Settled;
        evt.WinningSelectionId = job.WinningSelectionId;

        var marketId = await db.Markets
            .Where(m => m.EventId == job.EventId)
            .Select(m => m.Id)
            .FirstOrDefaultAsync();

        var bets = await db.Bets
            .Where(b => b.MarketId == marketId)
            .ToListAsync();

        foreach (var bet in bets)
        {
            if (bet.SelectionId == job.WinningSelectionId)
            {
                bet.State = BetState.Won;
                bet.Payout = bet.Stake * bet.Odds;
                bet.SettledAt = DateTime.UtcNow;

                var user = await db.Users.FindAsync(bet.UserId);
                if (user is not null)
                    user.Balance += bet.Payout.Value;
            }
            else
            {
                bet.State = BetState.Lost;
                bet.Payout = 0;
                bet.SettledAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }
}
