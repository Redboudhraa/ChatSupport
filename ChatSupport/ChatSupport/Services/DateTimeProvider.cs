// In Application/Services/DateTimeProvider.cs
using ChatSupport.Interfaces;

namespace ChatSupport.Application.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
