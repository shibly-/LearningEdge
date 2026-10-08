using AutoMapper;
using LearningEdge.Application.Common.Mappings;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Handlers.Organizations;
using LearningEdge.Application.Models.Queries.Organizations;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Tests.Application.Handlers.Tests;

public class OrganizationQueryHandlersTests
{
    [Fact]
    public async Task Handle_ShouldReturnActiveOrganizationsOrderedByName()
    {
        await using var context = CreateContext();
        var beta = new Organization("Beta");
        var hidden = new Organization("Alpha");
        hidden.SoftDelete();
        context.Organizations.AddRange(beta, hidden);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new OrganizationQueryHandlers(context, CreateMapper());
        var result = await handler.Handle(new GetOrganizationsQuery(), CancellationToken.None);

        Assert.True(result.Success);
        var organization = Assert.Single(result.Data!);
        Assert.Equal("Beta", organization.Name);
        Assert.Equal(beta.Id, organization.Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrganizationDoesNotExist()
    {
        await using var context = CreateContext();
        var handler = new OrganizationQueryHandlers(context, CreateMapper());

        var result = await handler.Handle(new GetOrganizationByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultErrorKind.NotFound, result.Kind);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<OrganizationProfile>());
        return configuration.CreateMapper();
    }
}
