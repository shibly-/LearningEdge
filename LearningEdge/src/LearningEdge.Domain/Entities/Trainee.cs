using LearningEdge.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LearningEdge.Domain.Entities;

public class Trainee: Entity
{    
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = String.Empty;    
    public string Email { get; private set; } = default!;
    public string Phone { get; private set; } = String.Empty;    
    public Department Department { get; private set; } = default!;
    public Organization Organization { get; private set; } = default!;

    public Trainee(string firstName, string lastName, string email, string phone, Department department, Organization organization)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        Department = department;
        Organization = organization;
    }

    public static Trainee Create(string firstName, string lastName, string email, string phone, Department department, Organization organization)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("A trainee must have a first name.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("A trainee must have an email address.");
        }

        return new Trainee(firstName.Trim(), lastName.Trim() ?? string.Empty, email.Trim(), phone?.Trim() ?? string.Empty, department, organization);
    }   

    public void Update(string firstName, string lastName, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("A trainee must have a first name.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("A trainee must have an email address.");
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim() ?? string.Empty;
        Email = email.Trim();
        Phone = phone?.Trim() ?? string.Empty;
    }
}
