namespace ChatSupport.Domain;

public class Agent
{
    public string AgentId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public AgentSeniority Seniority { get; set; }
    public List<string> ActiveChatIds { get; set; } = new();
    public bool IsOnShift { get; set; } = false;

    public int MaxCapacity
    {
        get
        {
            var multiplier = Seniority switch
            {
                AgentSeniority.Junior => 0.4,
                AgentSeniority.MidLevel => 0.6,
                AgentSeniority.Senior => 0.8,
                AgentSeniority.TeamLead => 0.5,
                _ => 0.4
            };
            return (int)Math.Floor(10 * multiplier);
        }
    }

    public bool IsAvailable => IsOnShift && ActiveChatIds.Count < MaxCapacity;
}
public enum AgentSeniority
{
    Junior,
    MidLevel,
    Senior,
    TeamLead
}