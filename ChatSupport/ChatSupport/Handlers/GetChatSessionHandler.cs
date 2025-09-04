using ChatSupport.Interfaces;
using ChatSupport.Queries;
using ChatSupport.Results;

namespace ChatSupport.Handlers;

public partial class StartChatSessionHandler
{
    public class GetChatSessionHandler : IQueryHandler<GetChatSessionQuery, ChatSessionResult>
    {
        private readonly IChatSessionRepository _sessionRepository;

        public GetChatSessionHandler(IChatSessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        public async Task<ChatSessionResult> HandleAsync(GetChatSessionQuery query)
        {
            var session = await _sessionRepository.GetByIdAsync(query.SessionId);
            return new ChatSessionResult
            {
                Session = session,
                Found = session != null
            };
        }
    }
}
