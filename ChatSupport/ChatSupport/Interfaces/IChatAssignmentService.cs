using ChatSupport.Domain;

namespace ChatSupport.Interfaces;

public interface IChatAssignmentService
{
    Task<List<Agent>> GetNextAvailableAgentAsync();
    Task AssignChatToAgentAsync(string sessionId, string agentId);
    Task ReleaseChatFromAgentAsync(string sessionId, string agentId);
}
