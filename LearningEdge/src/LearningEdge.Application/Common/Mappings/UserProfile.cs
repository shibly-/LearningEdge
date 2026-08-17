using AutoMapper;
using LearningEdge.Domain.Entities.Users;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Commands.Users;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Application.Common.Mappings;

public class UserProfile: Profile
{
    public UserProfile()
    {
        // Domain → DTO
        CreateMap<User, UserDTO>()
            .ConstructUsing(src => new UserDTO(
                src.Id,
                src.FirstName,
                src.LastName,
                src.Email,
                src.Role,
                src.OrganizationId
            ));

        // Command → Domain
        CreateMap<CreateUserCommand, User>()
             .ForMember(x => x.Id, opt => opt.Ignore())
             .IgnoreAllPropertiesWithAnInaccessibleSetter();
    }
}
