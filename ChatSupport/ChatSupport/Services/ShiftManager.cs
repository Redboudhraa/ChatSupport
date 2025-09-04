// In Application/Services/ShiftManager.cs
using ChatSupport.Domain;
using ChatSupport.Interfaces;

namespace ChatSupport.Application.Services;
public class ShiftManager : IShiftManager
{
    private readonly IAgentRepository _agentRepository;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly ILogger<ShiftManager> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;


    public ShiftManager(
        IAgentRepository agentRepository,
        IChatSessionRepository sessionRepository,
        ILogger<ShiftManager> logger,
        IDateTimeProvider dateTimeProvider)
    {
        _agentRepository = agentRepository;
        _sessionRepository = sessionRepository;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    // --- Core Shift Management ---
    public async Task UpdateAgentShiftsAsync()
    {
        // This logic remains the same as the previous version
        var allAgents = await _agentRepository.GetAllAgentsAsync();
        var currentHour = _dateTimeProvider.UtcNow.Hour;
        var teamA_Ids = new[] { "tl1", "m1", "m2", "j1" };
        var teamB_Ids = new[] { "s1", "m3", "j2", "j3" };
        var teamC_Ids = new[] { "m4", "m5" };
        var overflow_Ids = Enumerable.Range(1, 6).Select(i => $"of{i}").ToArray();

        string[] activeTeamIds;
        if (currentHour >= 8 && currentHour < 16) activeTeamIds = teamA_Ids;
        else if (currentHour >= 16 && currentHour < 24) activeTeamIds = teamB_Ids;
        else activeTeamIds = teamC_Ids;

        bool isOverflowActive = await ShouldOverflowBeActiveAsync(allAgents.Where(a => activeTeamIds.Contains(a.AgentId)).ToList());

        var allActiveIds = new List<string>(activeTeamIds);
        if (isOverflowActive)
        {
            allActiveIds.AddRange(overflow_Ids);
        }

        foreach (var agent in allAgents)
        {
            bool shouldBeOnShift = allActiveIds.Contains(agent.AgentId);
            if (agent.IsOnShift != shouldBeOnShift)
            {
                agent.IsOnShift = shouldBeOnShift;
                await _agentRepository.UpdateAsync(agent);
            }
        }
    }

    public async Task<List<Agent>> GetActiveTeamAgentsAsync()
    {
        return (await _agentRepository.GetAllAgentsAsync()).Where(a => a.IsOnShift).ToList();
    }



    public async Task<int> GetCurrentTeamCapacityAsync()
    {
        var activeAgents = await GetActiveTeamAgentsAsync();
        return activeAgents.Sum(a => a.MaxCapacity);
    }

    public async Task<int> GetMaxQueueSizeAsync()
    {
        // According to the rules, overflow does NOT contribute to the main queue size.
        // It provides an extra buffer *after* the main queue is full.
        var allAgents = await _agentRepository.GetAllAgentsAsync();
        var baseTeam = GetBaseShiftTeam(allAgents);
        var baseCapacity = baseTeam.Sum(a => a.MaxCapacity);
        return (int)Math.Floor(baseCapacity * 1.5);
    }

    public bool IsOfficeHours()
    {
        var now = _dateTimeProvider.UtcNow;
        return now.DayOfWeek >= DayOfWeek.Monday &&
               now.DayOfWeek <= DayOfWeek.Friday &&
               now.Hour >= 9 && now.Hour < 18; // Example: 9 AM to 6 PM UTC
    }


    private async Task<bool> ShouldOverflowBeActiveAsync(List<Agent> baseTeam)
    {
        var currentlyActiveOverflow = (await _agentRepository.GetAllAgentsAsync()).Any(a => a.AgentId.StartsWith("of") && a.IsOnShift);
        var mainTeamCapacity = baseTeam.Sum(a => a.MaxCapacity);
        var maxMainQueueSize = (int)Math.Floor(mainTeamCapacity * 1.5);
        var currentQueueSize = await _sessionRepository.GetQueueCountAsync();

        if (!currentlyActiveOverflow && IsOfficeHours())
        {
            if (currentQueueSize >= maxMainQueueSize) return true;
        }
        if (currentlyActiveOverflow)
        {
            var deactivationThreshold = (int)Math.Floor(maxMainQueueSize * 0.75);
            if (currentQueueSize < deactivationThreshold || !IsOfficeHours()) return false;
            return true;
        }
        return false;
    }

    private List<Agent> GetBaseShiftTeam(List<Agent> allAgents)
    {
        var currentHour = _dateTimeProvider.UtcNow.Hour;
        if (currentHour >= 9 && currentHour < 18) return allAgents.Where(a => new[] { "tl1", "m1", "m2", "j1" }.Contains(a.AgentId)).ToList();
        if (currentHour >= 18 && currentHour < 24) return allAgents.Where(a => new[] { "s1", "m3", "j2", "j3" }.Contains(a.AgentId)).ToList();
        return allAgents.Where(a => new[] { "m4", "m5" }.Contains(a.AgentId)).ToList();
    }
}