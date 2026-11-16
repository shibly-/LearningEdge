namespace LearningEdge.Application.Models.DTOs;

public record OrganizationDTO (
    Guid Id,
    string Name,
    string Description
);

/// <summary>Public registration dropdown. Name only — no description.</summary>
public record OrganizationChoice(Guid Id, string Name);

