using ChatSupport.Domain;
using ChatSupport.Interfaces;

public class FakeChatSessionRepository : IChatSessionRepository
{
    public int QueueCountToReturn { get; set; }
    public Task<int> GetQueueCountAsync() => Task.FromResult(QueueCountToReturn);
    public Task AddAsync(ChatSession session) => Task.CompletedTask;
    public Task<List<ChatSession>> GetSessionsAsync() => Task.FromResult(new List<ChatSession>());
    public Task UpdateAsync(ChatSession session) => Task.CompletedTask;
    public Task<ChatSession?> DequeueNextAsync() => Task.FromResult<ChatSession?>(null);
    public Task<ChatSession?> GetByIdAsync(string sessionId) => Task.FromResult<ChatSession?>(null);
    public Task RemoveAsync(string sessionId) => Task.CompletedTask;
    public Task<List<ChatSession>> GetAllSessionsAsync() => Task.FromResult(new List<ChatSession>());


}
