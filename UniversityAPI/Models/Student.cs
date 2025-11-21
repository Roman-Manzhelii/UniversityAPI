using System;
using System.Collections.Generic;

namespace UniversityAPI.Models;

public partial class Student
{
    public int Id { get; set; }

    public string StudentNumber { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int TotalCredits { get; set; }

    public int? DepartmentId { get; set; }

    public virtual Department? Department { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
