using LearningEdge.Domain.Entities.Questions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningEdge.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Question", "le");
        builder.UseTptMappingStrategy();

        builder.Property(question => question.Text)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasIndex(question => question.QuestionBankId);
        builder.HasQueryFilter(question => !question.IsDeleted);
    }
}

public sealed class McqQuestionConfiguration : IEntityTypeConfiguration<McqQuestion>
{
    public void Configure(EntityTypeBuilder<McqQuestion> builder)
    {
        builder.ToTable("McqQuestion", "le");

        builder.Property(question => question.Options)
            .HasConversion(QuestionValueConverters.Options, QuestionValueConverters.OptionsComparer)
            .HasColumnType("nvarchar(max)");

        builder.Property(question => question.Answer)
            .HasConversion(QuestionValueConverters.SingleChoice, QuestionValueConverters.SingleChoiceComparer)
            .HasColumnType("nvarchar(max)");
    }
}

public sealed class MultipleSelectionQuestionConfiguration : IEntityTypeConfiguration<MultipleSelectionQuestion>
{
    public void Configure(EntityTypeBuilder<MultipleSelectionQuestion> builder)
    {
        builder.ToTable("MultipleSelectionQuestion", "le");

        builder.Property(question => question.Options)
            .HasConversion(QuestionValueConverters.Options, QuestionValueConverters.OptionsComparer)
            .HasColumnType("nvarchar(max)");

        builder.Property(question => question.Answer)
            .HasConversion(QuestionValueConverters.MultipleChoice, QuestionValueConverters.MultipleChoiceComparer)
            .HasColumnType("nvarchar(max)");
    }
}

public sealed class TrueFalseQuestionConfiguration : IEntityTypeConfiguration<TrueFalseQuestion>
{
    public void Configure(EntityTypeBuilder<TrueFalseQuestion> builder)
    {
        builder.ToTable("TrueFalseQuestion", "le");

        builder.Property(question => question.Answer)
            .HasConversion(QuestionValueConverters.TrueFalse, QuestionValueConverters.TrueFalseComparer);
    }
}
