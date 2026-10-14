using AutoMapper;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.Entities.Exams;
using LearningEdge.Domain.Entities.Questions;
using LearningEdge.Domain.Entities.Trainings;

namespace LearningEdge.Application.Common.Mappings;

public class AssessmentProfile : Profile
{
    public AssessmentProfile()
    {
        CreateMap<QuestionBank, QuestionBankDTO>();
        CreateMap<PromptRequest, PromptRequestDTO>();
        CreateMap<Exam, ExamDTO>();
    }
}
