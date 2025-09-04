using ChatSupport.Domain;

namespace ChatSupport.Interfaces;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdAsync(string sessionId);
    Task<List<ChatSession>> GetSessionsAsync();
    Task AddAsync(ChatSession session);
    Task UpdateAsync(ChatSession session);
    Task RemoveAsync(string sessionId);
    Task<int> GetQueueCountAsync();
    Task<ChatSession?> DequeueNextAsync();
}
