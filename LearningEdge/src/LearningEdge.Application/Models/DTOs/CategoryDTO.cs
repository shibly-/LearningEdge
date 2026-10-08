namespace LearningEdge.Application.Models.DTOs;

public record CategoryDTO(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Description,
    bool IsActive
);
