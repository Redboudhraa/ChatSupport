using ChatSupport.Domain; // Your domain models namespace
using ChatSupport.Interfaces; // Your interfaces namespace

namespace ChatSupport.Services; // Your services namespace

public class ChatMonitoringService : BackgroundService
{
    private readonly ILogger<ChatMonitoringService> _logger;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatAssignmentService _assignmentService;
    private readonly IShiftManager _shiftManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    public ChatMonitoringService(
        ILogger<ChatMonitoringService> logger,
        IChatSessionRepository sessionRepository,
        IChatAssignmentService assignmentService,
        IShiftManager shiftManager,
        IDateTimeProvider dateTimeProvider
        )
    {
        _logger = logger;
        _sessionRepository = sessionRepository;
        _assignmentService = assignmentService;
        _shiftManager = shiftManager;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Chat Monitoring Service is starting.");

        // The ExecuteAsync method itself will return a long-running task.
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Insid Chat Monitoring Service is starting.");
                    await _shiftManager.UpdateAgentShiftsAsync();
                    await CleanupInactiveSessionsAsync();
                    await ProcessQueueAsync();
                }
                catch (OperationCanceledException)
                {
                    // This is expected when the application is shutting down.
                    // No need to log it as an error.
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unhandled error occurred in the Chat Monitoring Service.");
                }

                // Wait for the next cycle.
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }, stoppingToken);
    }

    /// <summary>
    /// Finds and removes sessions from the queue that have not been polled recently.
    /// </summary>
    private async Task CleanupInactiveSessionsAsync()
    {
        var cutoffTime = _dateTimeProvider.UtcNow.AddSeconds(-3);
        var queuedSessions = await _sessionRepository.GetSessionsAsync();

        var inactiveSessions = queuedSessions.Where(s => s.LastPollTime < cutoffTime).ToList();

        if (inactiveSessions.Any())
        {
            _logger.LogInformation("Found {Count} inactive sessions to clean up.", inactiveSessions.Count);
            foreach (var session in inactiveSessions)
            {
                // We don't need to update the status, as we are removing it immediately.
                await _sessionRepository.RemoveAsync(session.SessionId);
                _logger.LogWarning("Removed abandoned session {SessionId} due to polling timeout.", session.SessionId);
            }
        }
    }

    /// <summary>
    /// Assigns as many queued chats to as many available agents as possible in one cycle.
    /// </summary>
    private async Task ProcessQueueAsync()
    {
        // Get all agents who are currently on shift and have capacity.
        // The assignment service correctly sorts them by priority (Junior first, etc.).
        var availableAgents = await _assignmentService.GetNextAvailableAgentAsync();

        if (!availableAgents.Any())
        {
            // No agents available, nothing to do in this cycle.
            return;
        }

        // Loop through each available agent and try to assign them a chat from the queue.
        foreach (var agent in availableAgents)
        {
            // Try to get the next person from the waiting line (the FIFO queue).
            var nextSession = await _sessionRepository.DequeueNextAsync();

            // If DequeueNextAsync returns null, it means the queue is now empty.
            if (nextSession == null)
            {
                // We can stop processing even if there are more available agents.
                break; // Exit the foreach loop.
            }

            // If we have both an agent and a session, perform the assignment.
            _logger.LogInformation("Assigning session {SessionId} to agent {AgentName} ({AgentId})",
                nextSession.SessionId, agent.Name, agent.AgentId);

            await _assignmentService.AssignChatToAgentAsync(nextSession.SessionId, agent.AgentId);
            nextSession.Status = ChatSessionStatus.Active;
            nextSession.AssignedAgentId = agent.AgentId;
            await _sessionRepository.UpdateAsync(nextSession);
        }
    }
}