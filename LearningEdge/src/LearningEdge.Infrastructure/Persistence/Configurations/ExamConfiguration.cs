using LearningEdge.Domain.Entities.Exams;
using LearningEdge.Domain.Entities.Questions;
using LearningEdge.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningEdge.Infrastructure.Persistence.Configurations;

public sealed class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.ToTable("Exam", "le");

        builder.Property(exam => exam.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(exam => new { exam.TrainingId, exam.Title })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Ignore(exam => exam.Questions);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(exam => exam.PreparedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(exam => !exam.IsDeleted);
    }
}

public sealed class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> builder)
    {
        builder.ToTable("ExamQuestion", "le");
        builder.HasKey(link => new { link.ExamId, link.QuestionId });

        builder.HasOne<Exam>()
            .WithMany()
            .HasForeignKey(link => link.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Question>()
            .WithMany()
            .HasForeignKey(link => link.QuestionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
