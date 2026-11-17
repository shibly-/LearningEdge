namespace LearningEdge.Application.Models.DTOs;

/// <summary>
/// Public totals for the home-page charts. Counts only — no names or records.
/// </summary>
public record PlatformSnapshotDTO(
    int OrganizationsActive,
    int OrganizationsInactive,
    int LearnersActive,
    int LearnersPending,
    int LearnersInactive,
    int TrainingsActive,
    int TrainingsInactive);
