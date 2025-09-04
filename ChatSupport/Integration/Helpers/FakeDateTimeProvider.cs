using ChatSupport.Interfaces;
// Helper "Fake" classes for the test. Put these in the same file.
public class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; }
}
