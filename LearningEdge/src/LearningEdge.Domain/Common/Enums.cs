using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LearningEdge.Domain.Common;

public class Enums
{
    public enum UserRole
    {
        [EnumMember(Value = "Learner")]
        Learner = 1,
        [EnumMember(Value = "Instructor")]
        Instructor = 2,
        [EnumMember(Value = "OrgAdmin")]
        OrgAdmin = 3,
        [EnumMember(Value = "SysAdmin")]
        SysAdmin = 4
    }

    /// <summary>
    /// Self-registered learners stay <see cref="Pending"/> until an OrgAdmin or
    /// SysAdmin approves them. Approval is what creates the Keycloak user.
    /// </summary>
    public enum UserActivationStatus
    {
        Pending = 1,
        Active = 2,
        Inactive = 3
    }
}
