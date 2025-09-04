using ChatSupport.Domain;
using ChatSupport.Interfaces;
using System.Collections.Concurrent;

namespace ChatSupport.Repositories;

public class InMemoryAgentRepository : IAgentRepository
{
    private readonly ConcurrentDictionary<string, Agent> _agents = new();

    public InMemoryAgentRepository()
    {
        SeedAgents();
    }

    public Task<List<Agent>> GetAvailableAgentsAsync()
    {
        var available = _agents.Values.Where(a => a.IsAvailable).ToList();
        return Task.FromResult(available);
    }

    public Task<Agent?> GetByIdAsync(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public Task UpdateAsync(Agent agent)
    {
        _agents.TryUpdate(agent.AgentId, agent, _agents[agent.AgentId]);
        return Task.CompletedTask;
    }

    public Task<List<Agent>> GetAllAgentsAsync()
    {
        return Task.FromResult(_agents.Values.ToList());
    }

    private void SeedAgents()
    {
        // Team A: 1 Team Lead + 2 Mid + 1 Junior
        _agents.TryAdd("tl1", new Agent { AgentId = "tl1", Name = "Team Lead 1", Seniority = AgentSeniority.TeamLead });
        _agents.TryAdd("m1", new Agent { AgentId = "m1", Name = "Mid Level 1", Seniority = AgentSeniority.MidLevel });
        _agents.TryAdd("m2", new Agent { AgentId = "m2", Name = "Mid Level 2", Seniority = AgentSeniority.MidLevel });
        _agents.TryAdd("j1", new Agent { AgentId = "j1", Name = "Junior 1", Seniority = AgentSeniority.Junior });

        // Team B: 1 Senior + 1 Mid + 2 Junior
        _agents.TryAdd("s1", new Agent { AgentId = "s1", Name = "Senior 1", Seniority = AgentSeniority.Senior });
        _agents.TryAdd("m3", new Agent { AgentId = "m3", Name = "Mid Level 3", Seniority = AgentSeniority.MidLevel });
        _agents.TryAdd("j2", new Agent { AgentId = "j2", Name = "Junior 2", Seniority = AgentSeniority.Junior });
        _agents.TryAdd("j3", new Agent { AgentId = "j3", Name = "Junior 3", Seniority = AgentSeniority.Junior });

        // Team C: 2 Mid-Level
        _agents.TryAdd("m4", new Agent { AgentId = "m4", Name = "Mid Level 4", Seniority = AgentSeniority.MidLevel });
        _agents.TryAdd("m5", new Agent { AgentId = "m5", Name = "Mid Level 5", Seniority = AgentSeniority.MidLevel });

        for (int i = 1; i <= 6; i++)
        {
            var agentId = $"of{i}";
            _agents.TryAdd(agentId, new Agent
            {
                AgentId = agentId,
                Name = $"Overflow Junior {i}",
                Seniority = AgentSeniority.Junior
            });
        }
    }
}