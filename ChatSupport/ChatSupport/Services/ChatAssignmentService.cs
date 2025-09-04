using ChatSupport.Domain;
using ChatSupport.Interfaces;

namespace ChatSupport.Services;

public class ChatAssignmentService : IChatAssignmentService
{
    private readonly IShiftManager _shiftManager;
    private readonly IAgentRepository _agentRepository;
    public ChatAssignmentService(IShiftManager shiftManager, IAgentRepository agentRepository)
    {
        _shiftManager = shiftManager;
        _agentRepository = agentRepository;
    }

    private int GetSeniorityPriority(AgentSeniority seniority)
    {
        return seniority switch
        {
            AgentSeniority.Junior => 1,
            AgentSeniority.TeamLead => 2, // Team Lead is prioritized after Junior
            AgentSeniority.MidLevel => 3,
            AgentSeniority.Senior => 4,
            _ => 99
        };
    }

    public async Task<List<Agent>> GetNextAvailableAgentAsync()
    {
        var activeTeam = await _shiftManager.GetActiveTeamAgentsAsync();

        return activeTeam
            .Where(a => a.IsAvailable)
            .OrderBy(a => GetSeniorityPriority(a.Seniority))
            .ThenBy(a => a.ActiveChatIds.Count)
            .ToList(); // Return the whole list
    }

    public async Task AssignChatToAgentAsync(string sessionId, string agentId)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent != null && agent.IsAvailable)
        {
            agent.ActiveChatIds.Add(sessionId);
            await _agentRepository.UpdateAsync(agent);
        }
    }

    public async Task ReleaseChatFromAgentAsync(string sessionId, string agentId)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent != null)
        {
            agent.ActiveChatIds.Remove(sessionId);
            await _agentRepository.UpdateAsync(agent);
        }
    }
}