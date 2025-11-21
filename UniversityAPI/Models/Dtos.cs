using System.Collections.Generic;

namespace UniversityAPI.Models;

// Auth DTOs used by POST /api/auth/login
public class LoginRequestDto
{
    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = null!;

    public int ExpiresIn { get; set; }

    public string Role { get; set; } = null!;

    public int? StudentId { get; set; }
}

// Department DTOs used by GET /api/departments
public class DepartmentDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
}

// Course DTOs used by course endpoints
public class CourseSummaryDto
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int Credits { get; set; }

    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = null!;
}

public class CourseDetailsDto
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int Credits { get; set; }

    public DepartmentDto Department { get; set; } = null!;
}

public class CourseCreateDto
{
    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int Credits { get; set; }

    public int DepartmentId { get; set; }
}

public class CourseUpdateDto
{
    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int Credits { get; set; }

    public int DepartmentId { get; set; }
}

// Student DTOs used by student endpoints
public class StudentSummaryDto
{
    public int Id { get; set; }

    public string StudentNumber { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int TotalCredits { get; set; }

    public int? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }
}

public class StudentDetailsDto
{
    public int Id { get; set; }

    public string StudentNumber { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int TotalCredits { get; set; }

    public DepartmentDto? Department { get; set; }

    public List<StudentEnrollmentDto> Enrollments { get; set; } = new();
}

public class StudentCreateDto
{
    public string StudentNumber { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int? DepartmentId { get; set; }
}

public class StudentUpdateDto
{
    public string Name { get; set; } = null!;

    public int? DepartmentId { get; set; }
}

// Enrollment DTOs used by /api/students/{id}/courses endpoints
public class StudentEnrollmentDto
{
    public int CourseId { get; set; }

    public string CourseCode { get; set; } = null!;

    public string CourseTitle { get; set; } = null!;

    public int Credits { get; set; }

    public string? Grade { get; set; }
}

public class StudentEnrollmentCreateDto
{
    public int CourseId { get; set; }
}

public class StudentEnrollmentUpdateDto
{
    public string? Grade { get; set; }
}
