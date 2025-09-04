namespace ChatSupport.Domain;

public class ChatSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastPollTime { get; set; } = DateTime.UtcNow;
    public ChatSessionStatus Status { get; set; } = ChatSessionStatus.Queued;
    public int QueuePosition { get; set; }
    public string? AssignedAgentId { get; set; }
}
public enum ChatSessionStatus
{
    Queued,
    Active,
    Inactive
}