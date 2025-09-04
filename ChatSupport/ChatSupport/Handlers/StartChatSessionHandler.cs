using ChatSupport.Commands;
using ChatSupport.Domain;
using ChatSupport.Interfaces;
using ChatSupport.Results;

namespace ChatSupport.Handlers;

public partial class StartChatSessionHandler : ICommandHandler<StartChatSessionCommand, StartChatSessionResult>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IShiftManager _shiftManager;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StartChatSessionHandler(
        IChatSessionRepository sessionRepository,
        IChatAssignmentService assignmentService, // It's okay to keep it in the constructor for now
        IShiftManager capacityCalculator,
        IDateTimeProvider dateTimeProvider)
    {
        _sessionRepository = sessionRepository;
        _shiftManager = capacityCalculator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<StartChatSessionResult> HandleAsync(StartChatSessionCommand command)
    {
        // 1. Get current queue metrics from the authoritative sources.
        var currentQueueSize = await _sessionRepository.GetQueueCountAsync();
        var maxMainQueueSize = await _shiftManager.GetMaxQueueSizeAsync();

        // 2. Decide if we can accept the chat.
        bool canQueue = false;
        if (currentQueueSize < maxMainQueueSize)
        {
            // There is room in the main team's queue.
            canQueue = true;
        }
        else if (_shiftManager.IsOfficeHours())
        {
            // The main queue is full, but it's office hours, so we check the overflow buffer.
            // Rule: Overflow team is 6 Juniors. Capacity = 6 * 4 = 24. Queue buffer = 24 * 1.5 = 36.
            const int overflowQueueBuffer = 36;
            var totalQueueAllowed = maxMainQueueSize + overflowQueueBuffer;

            if (currentQueueSize < totalQueueAllowed)
            {
                canQueue = true;
            }
        }

        // 3. Based on the decision, either queue the session or reject it.
        if (canQueue)
        {
            // There is space, so create and queue the session.
            return await CreateAndQueueSession(command.UserId, currentQueueSize + 1);
        }
        else
        {
            // Both main and overflow queues are full, or it's outside office hours. Reject the chat.
            return new StartChatSessionResult
            {
                Success = false,
                ErrorMessage = "Chat queue is full. Please try again later."
            };
        }
    }

    /// <summary>
    /// This method is now simplified. Its only job is to create a new session
    /// with a 'Queued' status and add it to the repository.
    /// </summary>
    private async Task<StartChatSessionResult> CreateAndQueueSession(string userId, int queuePosition)
    {
        var session = new ChatSession
        {
            UserId = userId,
            QueuePosition = queuePosition,
            CreatedAt = _dateTimeProvider.UtcNow,
            LastPollTime = _dateTimeProvider.UtcNow,
            // The default status is ChatSessionStatus.Queued, which is what we want.
        };

        await _sessionRepository.AddAsync(session);

        // THE IMMEDIATE ASSIGNMENT LOGIC HAS BEEN REMOVED.
        // The ChatMonitoringService is now solely responsible for assigning chats.
        // This prevents race conditions and ensures the queue is processed fairly (FIFO).

        return new StartChatSessionResult
        {
            Success = true,
            SessionId = session.SessionId,
            // The session is now guaranteed to be in the queue, so we can return the position directly.
            QueuePosition = queuePosition
        };
    }
}