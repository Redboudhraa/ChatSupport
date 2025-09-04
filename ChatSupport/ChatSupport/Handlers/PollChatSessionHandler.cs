using ChatSupport.Commands;
using ChatSupport.Domain;
using ChatSupport.Interfaces;

namespace ChatSupport.Handlers;

public partial class StartChatSessionHandler
{
    public class PollChatSessionHandler : ICommandHandler<PollChatSessionCommand, bool>
    {
        private readonly IChatSessionRepository _sessionRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        public PollChatSessionHandler(IChatSessionRepository sessionRepository, IDateTimeProvider dateTimeProvider)
        {
            _sessionRepository = sessionRepository;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<bool> HandleAsync(PollChatSessionCommand command)
        {
            var session = await _sessionRepository.GetByIdAsync(command.SessionId);
            if (session == null || session.Status == ChatSessionStatus.Inactive)
                return false;

            session.LastPollTime = _dateTimeProvider.UtcNow;
            await _sessionRepository.UpdateAsync(session);
            return true;
        }
    }
}