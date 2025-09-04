using Autofac;
using ChatSupport.Application.Services;
using ChatSupport.Commands;
using ChatSupport.Handlers;
using ChatSupport.Interfaces;
using ChatSupport.Queries;
using ChatSupport.Repositories;
using ChatSupport.Results;
using ChatSupport.Services;
using static ChatSupport.Handlers.StartChatSessionHandler;

public class ChatSupportModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Repositories
        builder.RegisterType<InMemoryChatSessionRepository>()
            .As<IChatSessionRepository>()
            .SingleInstance();

        builder.RegisterType<InMemoryAgentRepository>()
            .As<IAgentRepository>()
            .SingleInstance();

        builder.RegisterType<DateTimeProvider>()
           .As<IDateTimeProvider>()
           .SingleInstance();

        builder.RegisterType<ChatAssignmentService>()
            .As<IChatAssignmentService>()
            .SingleInstance();
        builder.RegisterType<ShiftManager>()
            .As<IShiftManager>()
            .SingleInstance();
        // Command Handlers
        builder.RegisterType<StartChatSessionHandler>()
            .As<ICommandHandler<StartChatSessionCommand, StartChatSessionResult>>();

        builder.RegisterType<PollChatSessionHandler>()
            .As<ICommandHandler<PollChatSessionCommand, bool>>();

        // Query Handlers  
        builder.RegisterType<GetQueueStatusHandler>()
            .As<IQueryHandler<GetQueueStatusQuery, QueueStatusResult>>();

        builder.RegisterType<GetChatSessionHandler>()
            .As<IQueryHandler<GetChatSessionQuery, ChatSessionResult>>();

        // Background Services
        //builder.RegisterType<ChatMonitoringService>()
        //    .As<IHostedService>()
        //    .SingleInstance();
    }
}