using FluentValidation;
using LearningEdge.Application;
using LearningEdge.Application.Common.Behaviors;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Models.Commands.Organizations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LearningEdge.Tests.Application.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Send_ReturnsValidationFailure_AndDoesNotRequireADatabase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new CreateOrganizationCommand("", "A school"));

        Assert.False(result.Success);
        Assert.Equal(ResultErrorKind.Validation, result.Kind);
        Assert.Contains("Organization name is required.", result.Error);
    }
}
