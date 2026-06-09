namespace BettingService.Models;

public enum MarketState
{
    Open,
    Suspended,
    Closed
}

public class Market
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public MarketState State { get; set; } = MarketState.Open;
    public List<Selection> Selections { get; set; } = new();
}

public class Selection
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Odds { get; set; }
    public Guid MarketId { get; set; }
    public Market Market { get; set; } = null!;
}
