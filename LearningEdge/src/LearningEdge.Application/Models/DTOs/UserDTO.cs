
namespace LearningEdge.Application.Models.DTOs;

public record UserDTO (
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Role
);