using ChatSupport.Application.Services;
using ChatSupport.Commands;
using ChatSupport.Domain;
using ChatSupport.Handlers;
using ChatSupport.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;

public class EndToEndTests
{
    [Fact]
    public async Task StartChatHandler_WhenQueueIsFull_And_TimeIsControlled_ShouldReturnFalse()
    {
        // ARRANGE
        var fakeAssignmentService = new FakeChatAssignmentService();
        var fakeTimeProvider = new FakeDateTimeProvider
        {
            // Set the time to be outside office hours (a Sunday).
            UtcNow = new DateTime(2023, 10, 29, 10, 0, 0, DateTimeKind.Utc)
        };

        var fakeSessionRepo = new FakeChatSessionRepository
        {
            // Tell the repository the queue is full.
            QueueCountToReturn = 100
        };

        var fakeAgentRepo = new FakeAgentRepository
        {
            // Give the ShiftManager some agents to calculate capacity.
            AgentsToReturn = new List<Agent> { new Agent { AgentId = "s1", Seniority = AgentSeniority.Senior } }
        };


        var shiftManager = new ShiftManager(
            fakeAgentRepo,
            fakeSessionRepo,
            new NullLogger<ShiftManager>(),
            fakeTimeProvider
        );

        var handler = new StartChatSessionHandler(
            fakeSessionRepo,
            fakeAssignmentService,
            shiftManager,
            fakeTimeProvider
        );

        var command = new StartChatSessionCommand { UserId = "user-002" };

        // ACT
        var result = await handler.HandleAsync(command);

        // ASSERT
        Assert.NotNull(result);
        Assert.False(result.Success); // The handler should reject the chat.
        Assert.Contains("full", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SessionLifecycle_Create_Poll_Wait_ThenPollFails()
    {

        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();


        var stopwatch = Stopwatch.StartNew();

        //Act 1: Create a new chat session.
        Debug.WriteLine($"[{stopwatch.ElapsedMilliseconds}ms] Creating session...");
        var createCommand = new StartChatSessionCommand { UserId = "real-time-user" };
        var createContent = new StringContent(JsonConvert.SerializeObject(createCommand), Encoding.UTF8, "application/json");

        var createResponse = await client.PostAsync("/api/chat/start", createContent);
        createResponse.EnsureSuccessStatusCode();

        var createResult = JsonConvert.DeserializeObject<StartChatSessionResult>(await createResponse.Content.ReadAsStringAsync());
        var sessionId = createResult.SessionId;
        Assert.True(createResult.Success);

        // ACT 2: Poll immediately. This must succeed and updates the LastPollTime.   
        Debug.WriteLine($"[{stopwatch.ElapsedMilliseconds}ms] Polling for session {sessionId} (should succeed)...");
        var pollResponse1 = await client.PostAsync($"/api/chat/poll/{sessionId}", null);
        Assert.Equal(HttpStatusCode.OK, pollResponse1.StatusCode);


        // ACT 3: Wait in real-time for longer than the cleanup threshold.
        // The requirement is 3 seconds. To be safe, we'll wait 4 seconds.
        // During this time, the real ChatMonitoringService is running in the background.
        await Task.Delay(4000);

        // ACT 4: Poll again.
        // By now, the background service should have run and found our session
        // to be inactive (since its LastPollTime is now >3 seconds in the past).
        var pollResponse2 = await client.PostAsync($"/api/chat/poll/{sessionId}", null);

        // ASSERT
        stopwatch.Stop();

        // The session should have been cleaned up by the background service.
        // The API should report that the session is no longer found.
        Assert.Equal(HttpStatusCode.NotFound, pollResponse2.StatusCode);
    }

    [Fact]
    public async Task GetStatus_AfterFloodingQueue_CorrectlyActivatesOverflowBasedOnOfficeHours()
    {
        var now = DateTime.UtcNow;

        // --- Step 1: Determine if we are currently within "Office Hours". ---
        // This boolean is the key to our dynamic test.
        bool isOfficeHours = (now.DayOfWeek >= DayOfWeek.Monday &&
                              now.DayOfWeek <= DayOfWeek.Friday &&
                              now.Hour >= 9 &&
                              now.Hour < 18);

        // --- Step 2: Determine which team should be active and what their limits are. ---
        int expectedBaseCapacity;
        int requestsToSendToFillQueue;

        if (now.Hour >= 8 && now.Hour < 16) // Team A
        {
            expectedBaseCapacity = 21;
            requestsToSendToFillQueue = 31;
        }
        else if (now.Hour >= 16 && now.Hour < 24) // Team B
        {
            expectedBaseCapacity = 22;
            requestsToSendToFillQueue = 33;
        }
        else // Team C
        {
            expectedBaseCapacity = 12;
            requestsToSendToFillQueue = 18;
        }

        int expectedOverflowCapacity = isOfficeHours ? 24 : 0;

        // ACT: Start a clean server and flood it with requests.

        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < requestsToSendToFillQueue; i++)
        {
            var command = new StartChatSessionCommand { UserId = $"office-hours-test-user-{i}" };
            var content = new StringContent(JsonConvert.SerializeObject(command), Encoding.UTF8, "application/json");
            tasks.Add(client.PostAsync("/api/chat/start", content));
        }
        await Task.WhenAll(tasks);

        // Give the background service a moment to react to the full queue.
        await Task.Delay(2000);

        // Get the final status from the API.
        var statusResponse = await client.GetAsync("/api/chat/status");
        statusResponse.EnsureSuccessStatusCode();
        var statusResult = JsonConvert.DeserializeObject<QueueStatusResult>(await statusResponse.Content.ReadAsStringAsync());

        Assert.NotNull(statusResult);

        // --- ASSERTION 1 (The most important one) ---
        Assert.Equal(isOfficeHours, statusResult.OverflowActive);

        // --- ASSERTION 2 ---
        Assert.Equal(expectedBaseCapacity + expectedOverflowCapacity, statusResult.TotalCapacity);

        // --- ASSERTION 3 ---
        Assert.True(statusResult.CurrentQueueSize == requestsToSendToFillQueue, "Queue size should be close to the number of requests sent.");
    }
}