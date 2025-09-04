using ChatSupport.Domain;

namespace ChatSupport.Interfaces;

public interface IShiftManager
{
    Task UpdateAgentShiftsAsync();
    Task<List<Agent>> GetActiveTeamAgentsAsync();

    // Add the new responsibilities
    Task<int> GetCurrentTeamCapacityAsync();
    Task<int> GetMaxQueueSizeAsync();
    bool IsOfficeHours();
}
