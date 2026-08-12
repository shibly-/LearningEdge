using LearningEdge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.Entities;

public sealed class Organization: Entity
{    
    public string OrganizationName { get; private set; } = default!;
    public string OrganizationDescription { get; private set;} = default!;
    public string OrganizationType { get; private set; } = default!; 
    public Organization(string organizationName, string organizationDescription, string organizationType)
    {
        OrganizationName = organizationName;
        OrganizationDescription = organizationDescription;
        OrganizationType = organizationType;
    }

    public static Organization Create(string organizationName, string organizationDescription, string organizationType)
    {
        if (string.IsNullOrWhiteSpace(organizationName))
        {
            throw new DomainException("An organization must have a name.");
        }

        return new Organization(organizationName.Trim(), organizationDescription.Trim(), organizationType.Trim());
    }

    public void Update(string organizationName, string organizationDescription, string organizationType)
    {
        if (string.IsNullOrWhiteSpace(organizationName))
        {
            throw new DomainException("An organization must have a name.");
        }

        OrganizationName = organizationName.Trim();
        OrganizationDescription = organizationDescription.Trim();
        OrganizationType = organizationType.Trim();
    }
}  
