using ChatSupport.Domain;

namespace ChatSupport.Interfaces;

public interface IAgentRepository
{
    Task<List<Agent>> GetAvailableAgentsAsync();
    Task<Agent?> GetByIdAsync(string agentId);
    Task UpdateAsync(Agent agent);
    Task<List<Agent>> GetAllAgentsAsync();
}
