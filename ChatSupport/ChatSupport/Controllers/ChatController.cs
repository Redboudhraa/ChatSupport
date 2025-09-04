using ChatSupport.Commands;
using ChatSupport.Interfaces;
using ChatSupport.Queries;
using ChatSupport.Results;
using Microsoft.AspNetCore.Mvc;

namespace ChatSupport.Controlle
{
    /// <summary>
    /// Manages the lifecycle of chat sessions, including creation, polling, and status checks.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ICommandHandler<StartChatSessionCommand, StartChatSessionResult> _startChatHandler;
        private readonly ICommandHandler<PollChatSessionCommand, bool> _pollHandler;
        private readonly IQueryHandler<GetQueueStatusQuery, QueueStatusResult> _queueStatusHandler;
        private readonly IQueryHandler<GetChatSessionQuery, ChatSessionResult> _sessionHandler;

        public ChatController(
            ICommandHandler<StartChatSessionCommand, StartChatSessionResult> startChatHandler,
            ICommandHandler<PollChatSessionCommand, bool> pollHandler,
            IQueryHandler<GetQueueStatusQuery, QueueStatusResult> queueStatusHandler,
            IQueryHandler<GetChatSessionQuery, ChatSessionResult> sessionHandler)
        {
            _startChatHandler = startChatHandler;
            _pollHandler = pollHandler;
            _queueStatusHandler = queueStatusHandler;
            _sessionHandler = sessionHandler;
        }

        /// <summary>
        /// Initiates a new chat session.
        /// </summary>
        /// <remarks>
        /// This endpoint accepts a user's request to start a chat. The system will check if there is
        /// capacity in the queue. If so, a session is created and queued, and a session ID is returned.
        /// If the system is at full capacity, the request is rejected.
        /// </remarks>
        /// <param name="command">The command containing the user's ID.</param>
        /// <returns>A result object indicating success or failure, along with the new session ID and queue position.</returns>
        /// <response code="200">Returns the successfully created session details.</response>
        /// <response code="400">Returned if the chat queue is full and the request cannot be accepted.</response>
        [HttpPost("start")]
        public async Task<ActionResult<StartChatSessionResult>> StartChat([FromBody] StartChatSessionCommand command)
        {
            var result = await _startChatHandler.HandleAsync(command);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Submits a "keep-alive" poll for an active or queued chat session.
        /// </summary>
        /// <remarks>
        /// The client should call this endpoint every second after a session has been successfully created.
        /// This signal prevents the session from being marked as abandoned and removed by the system.
        /// </remarks>
        /// <param name="sessionId">The unique identifier of the chat session.</param>
        /// <returns>An HTTP 200 OK status if the session is found and active/queued. An HTTP 404 Not Found if the session does not exist or has been terminated.</returns>
        /// <response code="200">The session was found and its poll time was updated.</response>
        /// <response code="404">The specified session ID was not found or the session is inactive.</response>
        [HttpPost("poll/{sessionId}")]
        public async Task<ActionResult> Poll(string sessionId)
        {
            var command = new PollChatSessionCommand { SessionId = sessionId };
            var success = await _pollHandler.HandleAsync(command);

            if (!success)
                return NotFound("Session not found or inactive");

            return Ok();
        }

        /// <summary>
        /// Retrieves the current status of the overall chat system and queue.
        /// </summary>
        /// <remarks>
        /// This endpoint provides a snapshot of the system's load, including the current number of sessions,
        /// the maximum allowed queue size, the total agent capacity, and whether the overflow team is active.
        /// </remarks>
        /// <returns>A result object with the current queue and capacity metrics.</returns>
        /// <response code="200">Returns the current system status.</response>
        [HttpGet("status")]
        public async Task<ActionResult<QueueStatusResult>> GetQueueStatus()
        {
            var result = await _queueStatusHandler.HandleAsync(new GetQueueStatusQuery());
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the details of a specific chat session.
        /// </summary>
        /// <remarks>
        /// This can be used by the client to check the status of their specific session (e.g., Queued, Active)
        /// and to see which agent has been assigned to them.
        /// </remarks>
        /// <param name="sessionId">The unique identifier of the chat session.</param>
        /// <returns>The details of the requested chat session.</returns>
        /// <response code="200">Returns the found session details.</response>
        /// <response code="404">The specified session ID was not found.</response>
        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult<ChatSessionResult>> GetSession(string sessionId)
        {
            var query = new GetChatSessionQuery { SessionId = sessionId };
            var result = await _sessionHandler.HandleAsync(query);

            if (!result.Found)
                return NotFound();

            return Ok(result);
        }
    }
}