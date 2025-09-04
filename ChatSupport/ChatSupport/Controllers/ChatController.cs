using ChatSupport.Commands;
using ChatSupport.Interfaces;
using ChatSupport.Queries;
using ChatSupport.Results;
using Microsoft.AspNetCore.Mvc;

namespace ChatSupport.Controlle;
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

    [HttpPost("start")]
    public async Task<ActionResult<StartChatSessionResult>> StartChat([FromBody] StartChatSessionCommand command)
    {
        var result = await _startChatHandler.HandleAsync(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("poll/{sessionId}")]
    public async Task<ActionResult> Poll(string sessionId)
    {
        var command = new PollChatSessionCommand { SessionId = sessionId };
        var success = await _pollHandler.HandleAsync(command);

        if (!success)
            return NotFound("Session not found or inactive");

        return Ok();
    }

    [HttpGet("status")]
    public async Task<ActionResult<QueueStatusResult>> GetQueueStatus()
    {
        var result = await _queueStatusHandler.HandleAsync(new GetQueueStatusQuery());
        return Ok(result);
    }

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