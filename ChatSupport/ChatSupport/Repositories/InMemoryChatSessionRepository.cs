using ChatSupport.Domain;
using ChatSupport.Interfaces;
using System.Collections.Concurrent;

namespace ChatSupport.Repositories;

public class InMemoryChatSessionRepository : IChatSessionRepository
{
    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();
    private readonly ConcurrentQueue<string> _queueOrder = new();


    /// <summary>
    /// Atomically removes the next session ID from the queue and returns the full session object.
    /// It ensures FIFO (First-In, First-Out) processing.
    /// </summary>
    /// <returns>The next ChatSession in the queue, or null if the queue is empty.</returns>
    public Task<ChatSession?> DequeueNextAsync()
    {
        // Loop in case the dequeued session ID is for a session that was already removed or became inactive.
        while (_queueOrder.TryDequeue(out var sessionId))
        {
            // Check if the session still exists and is in the correct state.
            if (_sessions.TryGetValue(sessionId, out var session) &&
                session.Status == ChatSessionStatus.Queued)
            {
                // Found a valid, queued session. Return it.
                return Task.FromResult<ChatSession?>(session);
            }
            // If the session was not found or its status wasn't 'Queued', the loop
            // will automatically continue and try the next ID in the queue.
        }

        // If the loop finishes, it means the queue is empty.
        return Task.FromResult<ChatSession?>(null);
    }

    public Task<ChatSession?> GetByIdAsync(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<List<ChatSession>> GetSessionsAsync()
    {
        var queuedSessions = _sessions.Values
            .Where(s => s.Status != ChatSessionStatus.Inactive)
            .OrderBy(s => s.CreatedAt)
            .ToList();
        return Task.FromResult(queuedSessions);
    }

    public Task AddAsync(ChatSession session)
    {
        _sessions.TryAdd(session.SessionId, session);
        if (session.Status == ChatSessionStatus.Queued)
        {
            _queueOrder.Enqueue(session.SessionId);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ChatSession session)
    {
        _sessions.TryUpdate(session.SessionId, session, _sessions[session.SessionId]);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    public Task<int> GetQueueCountAsync()
    {
        var count = _sessions.Values.Count(s => s.Status != ChatSessionStatus.Inactive);
        return Task.FromResult(count);
    }

    public ChatSession? DequeueNext()
    {
        while (_queueOrder.TryDequeue(out var sessionId))
        {
            if (_sessions.TryGetValue(sessionId, out var session) &&
                session.Status == ChatSessionStatus.Queued)
            {
                return session;
            }
        }
        return null;
    }
}