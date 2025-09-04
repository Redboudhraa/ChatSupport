namespace ChatSupport.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}