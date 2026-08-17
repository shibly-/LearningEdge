
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Application.Models.DTOs;

public record UserDTO (
    Guid Id,
    string FirstName,
    string LastName,
    string Email,    
    UserRole Role,
    Guid OrganizationId
);