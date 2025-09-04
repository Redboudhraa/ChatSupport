using ChatSupport.Application.Services;
using ChatSupport.Domain;
using Microsoft.Extensions.Logging.Abstractions;

public class ShiftManager_UnitTests
{
    private readonly FakeDateTimeProvider _fakeTimeProvider;
    private readonly FakeChatSessionRepository _fakeSessionRepo;
    private readonly FakeAgentRepository _fakeAgentRepo;
    private readonly ShiftManager _shiftManager;

    // The constructor runs before each test, providing a clean slate.
    public ShiftManager_UnitTests()
    {
        _fakeTimeProvider = new FakeDateTimeProvider();
        _fakeSessionRepo = new FakeChatSessionRepository();
        _fakeAgentRepo = new FakeAgentRepository();
        _fakeAgentRepo.AgentsToReturn = CreateFullAgentList(); // Seed with all agents

        _shiftManager = new ShiftManager(
            _fakeAgentRepo,
            _fakeSessionRepo,
            new NullLogger<ShiftManager>(),
            _fakeTimeProvider
        );
    }


    [Fact]
    public async Task UpdateAgentShifts_DuringTeamB_Hours_ShouldActivateOnlyTeamB_Agents()
    {
        // ARRANGE
        // Set the time to be during Team B's shift (e.g., 20:00 UTC).
        _fakeTimeProvider.UtcNow = new DateTime(2023, 11, 1, 20, 0, 0, DateTimeKind.Utc);
        // Ensure the queue is not full, so overflow is not a factor.
        _fakeSessionRepo.QueueCountToReturn = 5;

        var teamB_Ids = new[] { "s1", "m3", "j2", "j3" };

        // ACT
        await _shiftManager.UpdateAgentShiftsAsync();

        // ASSERT
        var activeAgents = _fakeAgentRepo.AgentsToReturn.Where(a => a.IsOnShift).ToList();

        // There should be exactly 4 agents on shift.
        Assert.Equal(4, activeAgents.Count);
        // All active agents must be from Team B.
        Assert.All(activeAgents, agent => Assert.Contains(agent.AgentId, teamB_Ids));
    }


    [Fact]
    public async Task UpdateAgentShifts_WhenQueueIsFull_DuringOfficeHours_ShouldActivateOverflow()
    {
        // ARRANGE
        // Set the time to be during Team A's shift AND during office hours (e.g., Friday at 14:00 UTC).
        _fakeTimeProvider.UtcNow = new DateTime(2023, 11, 3, 14, 0, 0, DateTimeKind.Utc);

        // Set the queue count to be exactly at the trigger point for Team A (31).
        _fakeSessionRepo.QueueCountToReturn = 31;

        var teamA_Ids = new[] { "tl1", "m1", "m2", "j1" };

        // ACT
        await _shiftManager.UpdateAgentShiftsAsync();

        // ASSERT
        var activeAgents = _fakeAgentRepo.AgentsToReturn.Where(a => a.IsOnShift).ToList();

        // We expect Team A (4 agents) + Overflow Team (6 agents) = 10 agents.
        Assert.Equal(10, activeAgents.Count);
        // Check that all of Team A is active.
        Assert.All(teamA_Ids, id => Assert.True(activeAgents.Any(a => a.AgentId == id)));
        // Check that at least one overflow agent is active.
        Assert.True(activeAgents.Any(a => a.AgentId.StartsWith("of")));
    }


    [Fact]
    public async Task UpdateAgentShifts_WhenOverflowIsActive_AndQueueDrops_ShouldDeactivateOverflow()
    {
        // ARRANGE
        // Set the time to be during office hours.
        _fakeTimeProvider.UtcNow = new DateTime(2023, 11, 3, 14, 0, 0, DateTimeKind.Utc);

        // Set the initial state: pretend overflow was already active.
        _fakeAgentRepo.AgentsToReturn.First(a => a.AgentId.StartsWith("of")).IsOnShift = true;

        // Set the queue count far below the deactivation threshold (Team A's deactivation is 23).
        _fakeSessionRepo.QueueCountToReturn = 10;

        // ACT
        await _shiftManager.UpdateAgentShiftsAsync();

        // ASSERT
        var activeAgents = _fakeAgentRepo.AgentsToReturn.Where(a => a.IsOnShift).ToList();

        // Check that NO overflow agents are active anymore.
        Assert.False(activeAgents.Any(a => a.AgentId.StartsWith("of")));
        // Check that only Team A agents are now active.
        Assert.Equal(4, activeAgents.Count);
    }

    // Helper method to create a full list of agents for tests.
    private List<Agent> CreateFullAgentList()
    {
        return new List<Agent>
        {
            // Team A
            new Agent { AgentId = "tl1", Name = "Team Lead 1", Seniority = AgentSeniority.TeamLead },
            new Agent { AgentId = "m1", Name = "Mid Level 1", Seniority = AgentSeniority.MidLevel },
            new Agent { AgentId = "m2", Name = "Mid Level 2", Seniority = AgentSeniority.MidLevel },
            new Agent { AgentId = "j1", Name = "Junior 1", Seniority = AgentSeniority.Junior },
            // Team B
            new Agent { AgentId = "s1", Name = "Senior 1", Seniority = AgentSeniority.Senior },
            new Agent { AgentId = "m3", Name = "Mid Level 3", Seniority = AgentSeniority.MidLevel },
            new Agent { AgentId = "j2", Name = "Junior 2", Seniority = AgentSeniority.Junior },
            new Agent { AgentId = "j3", Name = "Junior 3", Seniority = AgentSeniority.Junior },
            // Team C
            new Agent { AgentId = "m4", Name = "Mid Level 4", Seniority = AgentSeniority.MidLevel },
            new Agent { AgentId = "m5", Name = "Mid Level 5", Seniority = AgentSeniority.MidLevel },
            // Overflow Team
            new Agent { AgentId = "of1", Name = "Overflow Junior 1", Seniority = AgentSeniority.Junior },
            new Agent { AgentId = "of2", Name = "Overflow Junior 2", Seniority = AgentSeniority.Junior },
            new Agent { AgentId = "of3", Name = "Overflow Junior 3", Seniority = AgentSeniority.Junior },
            new Agent { AgentId = "of4", Name = "Overflow Junior 4", Seniority = AgentSeniority.Junior },
            new Agent { AgentId = "of5", Name = "Overflow Junior 5", Seniority = AgentSeniority.Junior },
            new Agent { AgentId = "of6", Name = "Overflow Junior 6", Seniority = AgentSeniority.Junior }
        };
    }
}