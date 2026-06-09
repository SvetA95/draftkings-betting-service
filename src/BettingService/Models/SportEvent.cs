namespace BettingService.Models;

public enum EventState
{
    Live,
    Resulted,
    Settled
}

public class SportEvent
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public EventState State { get; set; } = EventState.Live;
    public Guid? WinningSelectionId { get; set; }
    public List<Market> Markets { get; set; } = new();
}
