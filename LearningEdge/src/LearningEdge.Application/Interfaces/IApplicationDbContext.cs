using Microsoft.EntityFrameworkCore;
using LearningEdge.Domain.Entities.Categories;
using LearningEdge.Domain.Entities.Exams;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Domain.Entities.Questions;
using LearningEdge.Domain.Entities.Trainings;
using LearningEdge.Domain.Entities.Users;

namespace LearningEdge.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }      
    DbSet<User> Users { get; }
    DbSet<Category> Categories { get; }
    DbSet<Training> Trainings { get; }
    DbSet<TrainingFile> TrainingFiles { get; }
    DbSet<QuestionBank> QuestionBanks { get; }
    DbSet<Question> Questions { get; }
    DbSet<PromptRequest> PromptRequests { get; }
    DbSet<Exam> Exams { get; }
    DbSet<ExamQuestion> ExamQuestions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
