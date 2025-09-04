using ChatSupport.Domain;
using ChatSupport.Interfaces;

public class FakeAgentRepository : IAgentRepository
{
    public List<Agent> AgentsToReturn { get; set; } = new List<Agent>();
    public Task<List<Agent>> GetAllAgentsAsync() => Task.FromResult(AgentsToReturn);
    public Task UpdateAsync(Agent agent) => Task.CompletedTask;
    public Task<List<Agent>> GetAvailableAgentsAsync() => Task.FromResult(new List<Agent>());
    public Task<Agent?> GetByIdAsync(string agentId) => Task.FromResult<Agent?>(null);
}
