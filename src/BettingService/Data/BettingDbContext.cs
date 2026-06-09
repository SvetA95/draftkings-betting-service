using BettingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BettingService.Data;

public class BettingDbContext : DbContext
{
    public BettingDbContext(DbContextOptions<BettingDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<Selection> Selections => Set<Selection>();
    public DbSet<Bet> Bets => Set<Bet>();
    public DbSet<SportEvent> Events => Set<SportEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(u => u.Balance).HasPrecision(18, 2);
        modelBuilder.Entity<Bet>().Property(b => b.Stake).HasPrecision(18, 2);
        modelBuilder.Entity<Bet>().Property(b => b.Odds).HasPrecision(18, 2);
        modelBuilder.Entity<Bet>().Property(b => b.Payout).HasPrecision(18, 2);
        modelBuilder.Entity<Selection>().Property(s => s.Odds).HasPrecision(18, 2);

        modelBuilder.Entity<Selection>()
            .HasOne(s => s.Market)
            .WithMany(m => m.Selections)
            .HasForeignKey(s => s.MarketId);

        modelBuilder.Entity<SportEvent>()
            .HasMany(e => e.Markets)
            .WithOne()
            .HasForeignKey(m => m.EventId);
    }
}
