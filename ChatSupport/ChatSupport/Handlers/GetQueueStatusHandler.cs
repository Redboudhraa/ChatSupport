using ChatSupport.Interfaces;
using ChatSupport.Queries;
using ChatSupport.Results;

namespace ChatSupport.Handlers;

public partial class StartChatSessionHandler
{
    public class GetQueueStatusHandler : IQueryHandler<GetQueueStatusQuery, QueueStatusResult>
    {
        private readonly IChatSessionRepository _sessionRepository;
        private readonly IShiftManager _shiftManager;

        public GetQueueStatusHandler(
            IChatSessionRepository sessionRepository,
            IShiftManager capacityCalculator)
        {
            _sessionRepository = sessionRepository;
            _shiftManager = capacityCalculator;
        }

        public async Task<QueueStatusResult> HandleAsync(GetQueueStatusQuery query)
        {
            var currentQueueSize = await _sessionRepository.GetQueueCountAsync();
            var isOfficeHours = _shiftManager.IsOfficeHours();
            var mainTeamMaxQueue = await _shiftManager.GetMaxQueueSizeAsync();

            // Get the full list of currently active agents (includes overflow if active).
            var activeAgents = await _shiftManager.GetActiveTeamAgentsAsync();
            var currentTotalCapacity = activeAgents.Sum(a => a.MaxCapacity);

            // Check if the overflow team is currently part of the active agents.
            bool isOverflowActive = activeAgents.Any(a => a.AgentId.StartsWith("of"));

            // The total allowed queue size depends on whether overflow is active.
            int totalMaxQueueSize = mainTeamMaxQueue;
            if (isOverflowActive)
            {
                const int overflowQueueBuffer = 36;
                totalMaxQueueSize += overflowQueueBuffer;
            }

            return new QueueStatusResult
            {
                CurrentQueueSize = currentQueueSize,
                MaxQueueSize = totalMaxQueueSize,
                TotalCapacity = currentTotalCapacity,
                IsOfficeHours = isOfficeHours,
                OverflowActive = isOverflowActive
            };
        }
    }
}