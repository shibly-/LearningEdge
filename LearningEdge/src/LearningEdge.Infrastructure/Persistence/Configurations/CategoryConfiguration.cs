using LearningEdge.Domain.Entities.Categories;
using LearningEdge.Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningEdge.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Category", "le");

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(category => category.Description)
            .IsRequired()
            .HasMaxLength(500);
        builder.HasIndex(category => new { category.OrganizationId, category.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne<Organization>()
            .WithMany(organization => organization.Categories)
            .HasForeignKey(category => category.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(category => category.Trainings)
            .WithOne()
            .HasForeignKey(training => training.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(category => category.Trainings)
            .HasField("_trainings")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(category => !category.IsDeleted);
    }
}
