using AutoMapper;
using LearningEdge.Domain.Entities.Users;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Commands.Users;

namespace LearningEdge.Application.Common.Mappings;

public class UserProfile: Profile
{
    public UserProfile()
    {
        // Domain → DTO
        CreateMap<User, UserDTO>();

        // Command → Domain
        CreateMap<CreateUserCommand, User>()
             .ForMember(x => x.Id, opt => opt.Ignore())
             .IgnoreAllPropertiesWithAnInaccessibleSetter();
        
    }
}
