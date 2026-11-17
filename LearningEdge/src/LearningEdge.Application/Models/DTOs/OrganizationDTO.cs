namespace LearningEdge.Application.Models.DTOs;

public record OrganizationDTO (
    Guid Id,
    string Name,
    string Description,
    bool IsActive
);

/// <summary>Public registration dropdown. Name only — no description.</summary>
public record OrganizationChoice(Guid Id, string Name);

