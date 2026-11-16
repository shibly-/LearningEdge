using LearningEdge.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User", "le");

        builder.Property(user => user.FirstName)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(user => user.LastName)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(user => user.Role)
            .IsRequired();
        builder.Property(user => user.ActivationStatus)
            .IsRequired()
            .HasDefaultValue(UserActivationStatus.Active);
        builder.Property(user => user.PendingPasswordProtected);
        builder.Property(user => user.KeycloakUserId)
            .HasMaxLength(36);

        builder.HasIndex(user => new { user.OrganizationId, user.Email })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasQueryFilter(user => !user.IsDeleted);
    }
}
