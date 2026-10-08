using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningEdge.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organization", "le");

        builder.Property(organization => organization.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(organization => organization.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(organization => organization.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasMany(organization => organization.Users)
            .WithOne()
            .HasForeignKey(user => user.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(organization => organization.Users)
            .HasField("_users")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(organization => !organization.IsDeleted);
    }
}
