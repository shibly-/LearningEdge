using LearningEdge.Domain.Common;
using LearningEdge.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.Entities;

public class Department: Entity
{    
    public string DepartmentName { get; private set; } = default!;
    public Organization Organization { get; private set; } = default!;
    public Department(string departmentName, Organization organization)
    {
        DepartmentName = departmentName;
        Organization = organization;
    }

    public static Department Create(string departmentName, Organization organization)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            throw new DomainException("A department must have a name.");
        }

        return new Department(departmentName.Trim(), organization);
    }

    public void Update(string departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            throw new DomainException("A department must have a name.");
        }

        DepartmentName = departmentName.Trim();
    }
} 