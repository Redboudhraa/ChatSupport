using ChatSupport.Domain;

namespace ChatSupport.Results;

public class ChatSessionResult
{
    public ChatSession? Session { get; set; }
    public bool Found { get; set; }
}
