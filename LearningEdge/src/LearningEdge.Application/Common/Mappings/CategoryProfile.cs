using AutoMapper;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.Entities.Categories;
using LearningEdge.Domain.Entities.Trainings;

namespace LearningEdge.Application.Common.Mappings;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDTO>();
    }
}

public class TrainingProfile : Profile
{
    public TrainingProfile()
    {
        CreateMap<Training, TrainingDTO>();
        CreateMap<TrainingFile, TrainingFileDTO>();
    }
}
