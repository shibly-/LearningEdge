using AutoMapper;
using FluentValidation;
using LearningEdge.Application;
using LearningEdge.Application.Common.Behaviors;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Organizations;
using LearningEdge.Application.Models.Commands.Users;
using LearningEdge.Domain.Entities.Users;
using LearningEdge.Infrastructure.Persistence;
using LearningEdge.Tests.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static LearningEdge.Domain.Common.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace LearningEdge.Tests.Application;

public class MediatorDispatchTests
{
    public static TheoryData<Type> RequestTypes()
    {
        var data = new TheoryData<Type>();
        foreach (var type in typeof(AssemblyMarker).Assembly.GetTypes()
                     .Where(type => type is { IsAbstract: false, IsInterface: false }
                         && type.GetInterfaces().Any(IsRequestInterface)))
        {
            data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public async Task EveryRequest_HasARegisteredHandler(Type requestType)
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var responseType = requestType.GetInterfaces().First(IsRequestInterface).GetGenericArguments()[0];
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

        Assert.NotNull(scope.ServiceProvider.GetService(handlerType));
    }

    [Fact]
    public async Task Send_UpdateCommands_ReachTheirHandlers()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var organizationId = (await mediator.Send(new CreateOrganizationCommand("Acme", "A school"))).Data;
        var userId = (await mediator.Send(new CreateUserCommand(
            "Ada", "Lovelace", "ada@acme.test", UserRole.OrgAdmin, organizationId))).Data;

        var organization = await mediator.Send(new UpdateOrganizationCommand(organizationId, "Acme Academy", "Renamed"));
        var user = await mediator.Send(new UpdateUserCommand(
            userId, "Ada", "King", "ada.king@acme.test", UserRole.OrgAdmin, organizationId));

        Assert.Equal(ResultErrorKind.None, organization.Kind);
        Assert.Equal("Acme Academy", organization.Data!.Name);
        Assert.Equal(ResultErrorKind.None, user.Kind);
        Assert.Equal("King", user.Data!.LastName);
    }

    private static bool IsRequestInterface(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequest<>);

    private static ServiceProvider BuildProvider()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<IFileStorage, InMemoryFileStorage>();
        services.AddSingleton<IMapper>(new MapperConfiguration(config =>
            config.AddMaps(typeof(AssemblyMarker).Assembly)).CreateMapper());
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
