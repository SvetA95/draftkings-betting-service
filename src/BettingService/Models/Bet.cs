namespace BettingService.Models;

public enum BetState
{
    Pending,
    Active,
    Won,
    Lost,
    Void
}

public class Bet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid MarketId { get; set; }
    public Guid SelectionId { get; set; }
    public string SelectionName { get; set; } = string.Empty;
    public decimal Stake { get; set; }
    public decimal Odds { get; set; }
    public BetState State { get; set; } = BetState.Pending;
    public decimal? Payout { get; set; }
    public DateTime PlacedAt { get; set; }
    public DateTime? SettledAt { get; set; }
}
