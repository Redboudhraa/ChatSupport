namespace ChatSupport.Results;

public class StartChatSessionResult
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public string? ErrorMessage { get; set; }
    public int QueuePosition { get; set; }
}
