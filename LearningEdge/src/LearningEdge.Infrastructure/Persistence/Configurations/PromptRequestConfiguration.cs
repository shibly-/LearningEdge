using LearningEdge.Domain.Entities.Questions;
using LearningEdge.Domain.Entities.Trainings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningEdge.Infrastructure.Persistence.Configurations;

public sealed class PromptRequestConfiguration : IEntityTypeConfiguration<PromptRequest>
{
    public void Configure(EntityTypeBuilder<PromptRequest> builder)
    {
        builder.ToTable("PromptRequest", "le");

        builder.Property(request => request.PromptText)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(request => request.QuestionType)
            .HasConversion<int>();

        builder.Property(request => request.Status)
            .HasConversion<int>();

        builder.HasIndex(request => request.QuestionBankId);

        builder.HasOne<Training>()
            .WithMany()
            .HasForeignKey(request => request.TrainingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<QuestionBank>()
            .WithMany()
            .HasForeignKey(request => request.QuestionBankId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(request => !request.IsDeleted);
    }
}
