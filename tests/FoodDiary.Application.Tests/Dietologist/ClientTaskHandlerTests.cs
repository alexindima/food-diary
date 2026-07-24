using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Dietologist.Commands.CancelClientTask;
using FoodDiary.Application.Dietologist.Commands.ChangeClientTaskStatus;
using FoodDiary.Application.Dietologist.Commands.CreateClientTask;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetClientTasksForDietologist;
using FoodDiary.Application.Dietologist.Queries.GetMyClientTasks;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

#pragma warning disable IDE0007, IDE0008, MA0003

namespace FoodDiary.Application.Tests.Dietologist;

[ExcludeFromCodeCoverage]
public sealed class ClientTaskHandlerTests {
    private static readonly DateTime UtcNow = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateClientTask_CreatesTaskAndNotifiesClient() {
        User dietologist = User.Create("dietologist@example.com", "hash");
        UserId clientId = UserId.New();
        IClientTaskRepository tasks = Substitute.For<IClientTaskRepository>();
        INotificationWriter notifications = Substitute.For<INotificationWriter>();
        var handler = new CreateClientTaskCommandHandler(
            tasks,
            CreateActiveInvitationRepository(dietologist.Id, clientId),
            CreateUserContext(dietologist),
            notifications,
            new FixedTimeProvider(UtcNow));
        DateTime dueAt = UtcNow.AddDays(1);

        Result<ClientTaskModel> result = await handler.Handle(
            new CreateClientTaskCommand(
                dietologist.Id.Value,
                clientId.Value,
                "  Keep a diary  ",
                "  Every meal  ",
                dueAt),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal("Keep a diary", result.Value.Title),
            () => Assert.Equal("Every meal", result.Value.Details),
            () => Assert.Equal(ClientTaskStatus.Open, result.Value.Status),
            () => Assert.False(result.Value.IsOverdue));
        await tasks.Received(1).AddAsync(
            Arg.Is<ClientTask>(task => task != null && task.ClientUserId == clientId),
            Arg.Any<CancellationToken>());
        await notifications.Received(1).AddAsync(
            Arg.Is<Notification>(notification => notification != null && notification.UserId == clientId),
            false,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CreateClientTask_ReturnsFailureForInvalidUserOrClient(
        bool failUser,
        bool emptyClient) {
        User dietologist = User.Create("dietologist@example.com", "hash");
        IUserContextService userContext = failUser
            ? CreateFailingUserContext()
            : CreateUserContext(dietologist);
        var handler = new CreateClientTaskCommandHandler(
            Substitute.For<IClientTaskRepository>(),
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            userContext,
            Substitute.For<INotificationWriter>(),
            new FixedTimeProvider(UtcNow));

        Result<ClientTaskModel> result = await handler.Handle(
            new CreateClientTaskCommand(
                dietologist.Id.Value,
                emptyClient ? Guid.Empty : Guid.NewGuid(),
                "Task",
                null,
                null),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task CreateClientTask_WhenRelationshipIsInactive_ReturnsAccessDenied() {
        User dietologist = User.Create("dietologist@example.com", "hash");
        var handler = new CreateClientTaskCommandHandler(
            Substitute.For<IClientTaskRepository>(),
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            CreateUserContext(dietologist),
            Substitute.For<INotificationWriter>(),
            new FixedTimeProvider(UtcNow));

        Result<ClientTaskModel> result = await handler.Handle(
            new CreateClientTaskCommand(dietologist.Id.Value, Guid.NewGuid(), "Task", null, null),
            CancellationToken.None);

        ResultAssert.Failure(result, Errors.Dietologist.AccessDenied.Code);
    }

    [Fact]
    public void CreateClientTaskValidator_ValidatesShape() {
        var validator = new CreateClientTaskCommandValidator();
        var invalid = validator.TestValidate(
            new CreateClientTaskCommand(null, Guid.Empty, "", new string('x', 2001), null));
        var valid = validator.TestValidate(
            new CreateClientTaskCommand(null, Guid.NewGuid(), new string('x', 200), null, null));

        Assert.Multiple(
            () => invalid.ShouldHaveValidationErrorFor(command => command.ClientUserId),
            () => invalid.ShouldHaveValidationErrorFor(command => command.Title),
            () => invalid.ShouldHaveValidationErrorFor(command => command.Details),
            () => valid.ShouldNotHaveAnyValidationErrors());
    }

    [Fact]
    public async Task CancelClientTask_CancelsAndNotifiesOnce() {
        User dietologist = User.Create("dietologist@example.com", "hash");
        UserId clientId = UserId.New();
        ClientTask task = ClientTask.Create(dietologist.Id, clientId, "Task", null, UtcNow.AddDays(-1));
        IClientTaskRepository tasks = CreateTaskRepository(task);
        INotificationWriter notifications = Substitute.For<INotificationWriter>();
        var handler = new CancelClientTaskCommandHandler(
            tasks,
            CreateActiveInvitationRepository(dietologist.Id, clientId),
            CreateUserContext(dietologist),
            notifications,
            new FixedTimeProvider(UtcNow));

        Result<ClientTaskModel> first = await handler.Handle(
            new CancelClientTaskCommand(dietologist.Id.Value, task.Id.Value),
            CancellationToken.None);
        Result<ClientTaskModel> second = await handler.Handle(
            new CancelClientTaskCommand(dietologist.Id.Value, task.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(first);
        ResultAssert.Success(second);
        Assert.Multiple(
            () => Assert.Equal(ClientTaskStatus.Cancelled, first.Value.Status),
            () => Assert.False(first.Value.IsOverdue));
        await notifications.Received(1).AddAsync(
            Arg.Is<Notification>(notification => notification != null && notification.UserId == clientId),
            false,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task CancelClientTask_RejectsInvalidAccessOrTask(
        bool failUser,
        bool emptyTask,
        bool missingTask) {
        User dietologist = User.Create("dietologist@example.com", "hash");
        IClientTaskRepository tasks = Substitute.For<IClientTaskRepository>();
        if (!missingTask) {
            ClientTask otherTask = ClientTask.Create(UserId.New(), UserId.New(), "Task", null, null);
            tasks.GetByIdAsync(
                    Arg.Any<ClientTaskId>(),
                    true,
                    Arg.Any<CancellationToken>())
                .Returns(otherTask);
        }
        var handler = new CancelClientTaskCommandHandler(
            tasks,
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            failUser ? CreateFailingUserContext() : CreateUserContext(dietologist),
            Substitute.For<INotificationWriter>(),
            new FixedTimeProvider(UtcNow));

        Result<ClientTaskModel> result = await handler.Handle(
            new CancelClientTaskCommand(dietologist.Id.Value, emptyTask ? Guid.Empty : Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task CancelClientTask_WhenRelationshipIsInactive_ReturnsAccessDenied() {
        User dietologist = User.Create("dietologist@example.com", "hash");
        ClientTask task = ClientTask.Create(dietologist.Id, UserId.New(), "Task", null, null);
        var handler = new CancelClientTaskCommandHandler(
            CreateTaskRepository(task),
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            CreateUserContext(dietologist),
            Substitute.For<INotificationWriter>(),
            new FixedTimeProvider(UtcNow));

        Result<ClientTaskModel> result = await handler.Handle(
            new CancelClientTaskCommand(dietologist.Id.Value, task.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result, Errors.Dietologist.AccessDenied.Code);
    }

    [Fact]
    public async Task ChangeClientTaskStatus_CompletesAndReopensWithNotifications() {
        User client = User.Create("client@example.com", "hash");
        UserId dietologistId = UserId.New();
        ClientTask task = ClientTask.Create(dietologistId, client.Id, "Task", null, UtcNow.AddDays(-1));
        INotificationWriter notifications = Substitute.For<INotificationWriter>();
        var handler = new ChangeClientTaskStatusCommandHandler(
            CreateTaskRepository(task),
            CreateActiveInvitationRepository(dietologistId, client.Id),
            CreateUserContext(client),
            notifications,
            new FixedTimeProvider(UtcNow));

        Result<ClientTaskModel> completed = await handler.Handle(
            new ChangeClientTaskStatusCommand(client.Id.Value, task.Id.Value, "Completed"),
            CancellationToken.None);
        Result<ClientTaskModel> unchanged = await handler.Handle(
            new ChangeClientTaskStatusCommand(client.Id.Value, task.Id.Value, "completed"),
            CancellationToken.None);
        Result<ClientTaskModel> reopened = await handler.Handle(
            new ChangeClientTaskStatusCommand(client.Id.Value, task.Id.Value, "Open"),
            CancellationToken.None);

        ResultAssert.Success(completed);
        ResultAssert.Success(unchanged);
        ResultAssert.Success(reopened);
        Assert.Multiple(
            () => Assert.Equal(ClientTaskStatus.Completed, completed.Value.Status),
            () => Assert.False(completed.Value.IsOverdue),
            () => Assert.Equal(ClientTaskStatus.Open, reopened.Value.Status),
            () => Assert.True(reopened.Value.IsOverdue));
        await notifications.Received(2).AddAsync(
            Arg.Is<Notification>(notification => notification != null && notification.UserId == dietologistId),
            false,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, false, "Open")]
    [InlineData(false, true, "Open")]
    [InlineData(false, false, "Cancelled")]
    [InlineData(false, false, "not-a-status")]
    public async Task ChangeClientTaskStatus_RejectsInvalidInput(
        bool failUser,
        bool emptyTask,
        string status) {
        User client = User.Create("client@example.com", "hash");
        UserId dietologistId = UserId.New();
        ClientTask task = ClientTask.Create(dietologistId, client.Id, "Task", null, null);
        var handler = new ChangeClientTaskStatusCommandHandler(
            CreateTaskRepository(task),
            CreateActiveInvitationRepository(dietologistId, client.Id),
            failUser ? CreateFailingUserContext() : CreateUserContext(client),
            Substitute.For<INotificationWriter>(),
            new FixedTimeProvider(UtcNow));

        Result<ClientTaskModel> result = await handler.Handle(
            new ChangeClientTaskStatusCommand(client.Id.Value, emptyTask ? Guid.Empty : task.Id.Value, status),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task ChangeClientTaskStatus_RejectsMissingForeignAndInaccessibleTask() {
        User client = User.Create("client@example.com", "hash");
        ClientTask foreign = ClientTask.Create(UserId.New(), UserId.New(), "Task", null, null);
        var missingHandler = new ChangeClientTaskStatusCommandHandler(
            Substitute.For<IClientTaskRepository>(),
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            CreateUserContext(client),
            Substitute.For<INotificationWriter>(),
            new FixedTimeProvider(UtcNow));
        var foreignHandler = new ChangeClientTaskStatusCommandHandler(
            CreateTaskRepository(foreign),
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            CreateUserContext(client),
            Substitute.For<INotificationWriter>(),
            new FixedTimeProvider(UtcNow));
        ClientTask own = ClientTask.Create(UserId.New(), client.Id, "Task", null, null);
        var inaccessibleHandler = new ChangeClientTaskStatusCommandHandler(
            CreateTaskRepository(own),
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            CreateUserContext(client),
            Substitute.For<INotificationWriter>(),
            new FixedTimeProvider(UtcNow));

        Result<ClientTaskModel> missing = await missingHandler.Handle(
            new ChangeClientTaskStatusCommand(client.Id.Value, Guid.NewGuid(), "Open"),
            CancellationToken.None);
        Result<ClientTaskModel> foreignResult = await foreignHandler.Handle(
            new ChangeClientTaskStatusCommand(client.Id.Value, foreign.Id.Value, "Open"),
            CancellationToken.None);
        Result<ClientTaskModel> inaccessible = await inaccessibleHandler.Handle(
            new ChangeClientTaskStatusCommand(client.Id.Value, own.Id.Value, "Open"),
            CancellationToken.None);

        Assert.Multiple(
            () => ResultAssert.Failure(missing),
            () => ResultAssert.Failure(foreignResult),
            () => ResultAssert.Failure(inaccessible, Errors.Dietologist.AccessDenied.Code));
    }

    [Fact]
    public void ChangeClientTaskStatusValidator_ValidatesShape() {
        var validator = new ChangeClientTaskStatusCommandValidator();
        var invalid = validator.TestValidate(
            new ChangeClientTaskStatusCommand(null, Guid.Empty, "Cancelled"));
        var valid = validator.TestValidate(
            new ChangeClientTaskStatusCommand(null, Guid.NewGuid(), "completed"));

        Assert.Multiple(
            () => invalid.ShouldHaveValidationErrorFor(command => command.TaskId),
            () => invalid.ShouldHaveValidationErrorFor(command => command.Status),
            () => valid.ShouldNotHaveAnyValidationErrors());
    }

    [Fact]
    public async Task GetMyClientTasks_MapsOverdueState() {
        User client = User.Create("client@example.com", "hash");
        IClientTaskRepository tasks = Substitute.For<IClientTaskRepository>();
        tasks.GetByClientAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns([
                CreateReadModel(UserId.New(), client.Id, ClientTaskStatus.Open, UtcNow.AddMinutes(-1)),
                CreateReadModel(UserId.New(), client.Id, ClientTaskStatus.Completed, UtcNow.AddMinutes(-1)),
            ]);
        var handler = new GetMyClientTasksQueryHandler(
            tasks,
            CreateUserContext(client),
            new FixedTimeProvider(UtcNow));

        Result<IReadOnlyList<ClientTaskModel>> result = await handler.Handle(
            new GetMyClientTasksQuery(client.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Collection(
            result.Value,
            task => Assert.True(task.IsOverdue),
            task => Assert.False(task.IsOverdue));
    }

    [Fact]
    public async Task GetMyClientTasks_WhenAccessFails_ReturnsFailure() {
        var handler = new GetMyClientTasksQueryHandler(
            Substitute.For<IClientTaskRepository>(),
            CreateFailingUserContext(),
            new FixedTimeProvider(UtcNow));

        Result<IReadOnlyList<ClientTaskModel>> result = await handler.Handle(
            new GetMyClientTasksQuery(Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetClientTasksForDietologist_ReturnsMappedTasks() {
        User dietologist = User.Create("dietologist@example.com", "hash");
        UserId clientId = UserId.New();
        IClientTaskRepository tasks = Substitute.For<IClientTaskRepository>();
        tasks.GetByDietologistAndClientAsync(dietologist.Id, clientId, Arg.Any<CancellationToken>())
            .Returns([CreateReadModel(dietologist.Id, clientId, ClientTaskStatus.Open, null)]);
        var handler = new GetClientTasksForDietologistQueryHandler(
            tasks,
            CreateUserContext(dietologist),
            new FixedTimeProvider(UtcNow));

        Result<IReadOnlyList<ClientTaskModel>> result = await handler.Handle(
            new GetClientTasksForDietologistQuery(dietologist.Id.Value, clientId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        ClientTaskModel task = Assert.Single(result.Value);
        Assert.Multiple(
            () => Assert.Equal(clientId.Value, task.ClientUserId),
            () => Assert.False(task.IsOverdue));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetClientTasksForDietologist_RejectsInvalidAccessOrClient(
        bool failUser,
        bool emptyClient) {
        User dietologist = User.Create("dietologist@example.com", "hash");
        var handler = new GetClientTasksForDietologistQueryHandler(
            Substitute.For<IClientTaskRepository>(),
            failUser ? CreateFailingUserContext() : CreateUserContext(dietologist),
            new FixedTimeProvider(UtcNow));

        Result<IReadOnlyList<ClientTaskModel>> result = await handler.Handle(
            new GetClientTasksForDietologistQuery(
                dietologist.Id.Value,
                emptyClient ? Guid.Empty : Guid.NewGuid()),
            CancellationToken.None);

        if (failUser || emptyClient) {
            ResultAssert.Failure(result);
        } else {
            ResultAssert.Success(result);
        }
    }

    private static IClientTaskRepository CreateTaskRepository(ClientTask task) {
        IClientTaskRepository repository = Substitute.For<IClientTaskRepository>();
        repository.GetByIdAsync(task.Id, true, Arg.Any<CancellationToken>())
            .Returns(task);
        return repository;
    }

    private static IUserContextService CreateUserContext(User user) {
        IUserContextService service = Substitute.For<IUserContextService>();
        service.EnsureCanAccessAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((Error?)null);
        service.GetAccessibleUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success(user));
        return service;
    }

    private static IUserContextService CreateFailingUserContext() {
        IUserContextService service = Substitute.For<IUserContextService>();
        service.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Errors.Authentication.InvalidToken);
        return service;
    }

    private static IDietologistInvitationReadModelRepository CreateActiveInvitationRepository(
        UserId dietologistId,
        UserId clientId) {
        IDietologistInvitationReadModelRepository repository = Substitute.For<IDietologistInvitationReadModelRepository>();
        repository.GetActiveByClientAndDietologistReadModelAsync(
                clientId,
                dietologistId,
                Arg.Any<CancellationToken>())
            .Returns(new DietologistInvitationReadModel(
                Guid.NewGuid(),
                clientId.Value,
                dietologistId.Value,
                "dietologist@example.com",
                "client@example.com",
                null,
                null,
                null,
                null,
                null,
                null,
                ActivityLevel.Moderate,
                "dietologist@example.com",
                null,
                null,
                DietologistInvitationStatus.Accepted,
                new DietologistPermissionsReadModel(true, true, true, true, true, true, true, true),
                UtcNow.AddDays(-10),
                UtcNow.AddDays(10),
                UtcNow.AddDays(-9)));
        return repository;
    }

    private static ClientTaskReadModel CreateReadModel(
        UserId dietologistId,
        UserId clientId,
        ClientTaskStatus status,
        DateTime? dueAtUtc) =>
        new(
            Guid.NewGuid(),
            dietologistId.Value,
            clientId.Value,
            "Task",
            "Details",
            dueAtUtc,
            status,
            UtcNow.AddDays(-2),
            status == ClientTaskStatus.Open ? null : UtcNow.AddDays(-1));

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
