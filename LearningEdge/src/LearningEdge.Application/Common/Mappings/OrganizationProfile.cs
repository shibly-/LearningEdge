using AutoMapper;
using LearningEdge.Domain.Entities.Organizations;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Commands.Organizations;

namespace LearningEdge.Application.Common.Mappings;

public class OrganizationProfile : Profile
{
    public OrganizationProfile()
    {
        // Domain → DTO
        CreateMap<Organization, OrganizationDTO>();

        // Command → Domain
        CreateMap<CreateOrganizationCommand, Organization>()
             .ForMember(x => x.Id, opt => opt.Ignore())
             .IgnoreAllPropertiesWithAnInaccessibleSetter();
    }
}       

