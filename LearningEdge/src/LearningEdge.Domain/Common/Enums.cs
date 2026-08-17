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
}
