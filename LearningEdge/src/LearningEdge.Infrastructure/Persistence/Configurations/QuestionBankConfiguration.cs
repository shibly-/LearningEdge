using LearningEdge.Domain.Entities.Questions;
using LearningEdge.Domain.Entities.Trainings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningEdge.Infrastructure.Persistence.Configurations;

public sealed class QuestionBankConfiguration : IEntityTypeConfiguration<QuestionBank>
{
    public void Configure(EntityTypeBuilder<QuestionBank> builder)
    {
        builder.ToTable("QuestionBank", "le");

        builder.Property(bank => bank.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(bank => new { bank.TrainingId, bank.Title })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne<Training>()
            .WithMany()
            .HasForeignKey(bank => bank.TrainingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(bank => bank.Questions)
            .WithOne()
            .HasForeignKey(question => question.QuestionBankId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(bank => bank.Questions)
            .HasField("_questions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(bank => !bank.IsDeleted);
    }
}
