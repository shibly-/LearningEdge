using LearningEdge.Domain.Entities.Exams;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.Entities.Trainings;
using LearningEdge.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningEdge.Infrastructure.Persistence.Configurations;

public sealed class TrainingConfiguration : IEntityTypeConfiguration<Training>
{
    public void Configure(EntityTypeBuilder<Training> builder)
    {
        builder.ToTable("Training", "le");

        builder.Property(training => training.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(training => training.Description)
            .IsRequired()
            .HasMaxLength(500);
        builder.HasIndex(training => new { training.CategoryId, training.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // NoAction: Organization already cascades to Training through Category, and SQL Server
        // rejects a second cascade path to the same table.
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(training => training.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(training => training.Files)
            .WithOne()
            .HasForeignKey(file => file.TrainingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(training => training.Files)
            .HasField("_files")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(training => training.Exams)
            .WithOne()
            .HasForeignKey(exam => exam.TrainingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(training => training.Exams)
            .HasField("_exams")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(training => !training.IsDeleted);
    }
}

public sealed class TrainingFileConfiguration : IEntityTypeConfiguration<TrainingFile>
{
    public void Configure(EntityTypeBuilder<TrainingFile> builder)
    {
        builder.ToTable("TrainingFile", "le");

        builder.Property(file => file.FileName)
            .IsRequired()
            .HasMaxLength(255);
        builder.Property(file => file.ContentType)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(file => file.StorageKey)
            .IsRequired()
            .HasMaxLength(400);

        // NoAction: Organization already cascades to both User and (via Category/Training) TrainingFile,
        // and SQL Server rejects multiple cascade paths to the same table.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(file => file.UploadedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(file => !file.IsDeleted);
    }
}
