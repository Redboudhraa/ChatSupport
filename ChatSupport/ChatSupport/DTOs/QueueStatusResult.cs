namespace ChatSupport.Results;

public class QueueStatusResult
{
    public int CurrentQueueSize { get; set; }
    public int MaxQueueSize { get; set; }
    public int TotalCapacity { get; set; }
    public bool IsOfficeHours { get; set; }
    public bool OverflowActive { get; set; }
}
