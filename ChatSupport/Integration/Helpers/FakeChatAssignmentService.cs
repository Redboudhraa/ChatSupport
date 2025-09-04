using ChatSupport.Domain;
using ChatSupport.Interfaces;

public class FakeChatAssignmentService : IChatAssignmentService
{
    public Task AssignChatToAgentAsync(string sessionId, string agentId) => Task.CompletedTask;
    public Task<List<Agent>> GetNextAvailableAgentAsync() => Task.FromResult(new List<Agent>());
    public Task ReleaseChatFromAgentAsync(string sessionId, string agentId) => Task.CompletedTask;
}
