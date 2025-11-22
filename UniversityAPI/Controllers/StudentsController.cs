using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityAPI.Models;

namespace UniversityAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly UniversityContext _context;

    public StudentsController(UniversityContext context)
    {
        _context = context;
    }

    // GET: api/students
    // Admin: list all students with basic info
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<StudentSummaryDto>>> GetStudents()
    {
        var students = await _context.Students
            .AsNoTracking() // For read-only operation to make request lighter for CPU, RAM
            .Include(s => s.Department)
            .Select(s => new StudentSummaryDto
            {
                Id = s.Id,
                StudentNumber = s.StudentNumber,
                Name = s.Name,
                TotalCredits = s.TotalCredits,
                DepartmentId = s.DepartmentId,
                DepartmentName = s.Department != null ? s.Department.Name : null
            })
            .ToListAsync();

        return Ok(students);
    }

    // GET: api/students/{id}
    // Admin: can access any student; Student: can access only own record
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<StudentDetailsDto>> GetStudent(int id)
    {
        if (!CanAccessStudent(id))
        {
            return Forbid();
        }

        var student = await FindStudentDetailsAsync(id);

        if (student == null)
        {
            return NotFound();
        }

        return Ok(student);
    }

    // Reads studentId claim from JWT so we can chrck if student accesses own data
    private int? GetCurrentStudentIdFromToken()
    {
        var claim = User.FindFirst("studentId");
        if (claim == null)
        {
            return null;
        }

        int id;
        if (int.TryParse(claim.Value, out id))
        {
            return id;
        }

        return null;
    }

    // Allows Admin to access any student and Student only own record
    private bool CanAccessStudent(int studentId)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        var currentStudentId = GetCurrentStudentIdFromToken();
        if (!currentStudentId.HasValue)
        {
            return false;
        }

        return currentStudentId.Value == studentId;
    }

    // Helper to load full student details with department and enrollments
    private async Task<StudentDetailsDto?> FindStudentDetailsAsync(int id)
    {
        var student = await _context.Students
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StudentDetailsDto
            {
                Id = s.Id,
                StudentNumber = s.StudentNumber,
                Name = s.Name,
                TotalCredits = s.TotalCredits,
                Department = s.Department == null
                    ? null
                    : new DepartmentDto
                    {
                        Id = s.Department.Id,
                        Name = s.Department.Name
                    },
                Enrollments = s.Enrollments
                    .Select(e => new StudentEnrollmentDto
                    {
                        CourseId = e.CourseId,
                        CourseCode = e.Course.Code,
                        CourseTitle = e.Course.Title,
                        Credits = e.Course.Credits,
                        Grade = e.Grade
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync();

        return student;
    }
}
