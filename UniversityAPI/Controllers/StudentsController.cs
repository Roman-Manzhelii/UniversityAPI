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

    // POST: api/students
    // Admin: create a new student
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StudentDetailsDto>> CreateStudent([FromBody] StudentCreateDto dto)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        var student = new Student
        {
            StudentNumber = dto.StudentNumber,
            Name = dto.Name,
            TotalCredits = 0,
            DepartmentId = dto.DepartmentId
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var details = await FindStudentDetailsAsync(student.Id);
        if (details == null)
        {
            return StatusCode(500);
        }

        return CreatedAtAction(nameof(GetStudent), new { id = details.Id }, details);
    }

    // PUT: api/students/{id}
    // Admin can update any student,but Student can update only himself
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<StudentDetailsDto>> UpdateStudent(int id, [FromBody] StudentUpdateDto dto)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        if (!CanAccessStudent(id))
        {
            return Forbid();
        }

        var student = await _context.Students.FindAsync(id);
        if (student == null)
        {
            return NotFound();
        }

        student.Name = dto.Name;
        student.DepartmentId = dto.DepartmentId;

        await _context.SaveChangesAsync();

        var details = await FindStudentDetailsAsync(id);
        if (details == null)
        {
            return StatusCode(500);
        }

        return Ok(details);
    }

    // GET: api/students/{id}/courses
    // Admin can see courses for any student, but Student see only own courses
    [HttpGet("{id:int}/courses")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<StudentEnrollmentDto>>> GetStudentCourses(int id)
    {
        if (!CanAccessStudent(id))
        {
            return Forbid();
        }

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == id)
            .Select(e => new StudentEnrollmentDto
            {
                CourseId = e.CourseId,
                CourseCode = e.Course.Code,
                CourseTitle = e.Course.Title,
                Credits = e.Course.Credits,
                Grade = e.Grade
            })
            .ToListAsync();

        return Ok(enrollments);
    }

    // POST: api/students/{id}/courses
    // Admin can enroll any student, but Student enroll only self
    [HttpPost("{id:int}/courses")]
    [Authorize]
    public async Task<ActionResult<StudentEnrollmentDto>> EnrollStudentInCourse(
        int id,
        [FromBody] StudentEnrollmentCreateDto dto)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        if (!CanAccessStudent(id))
        {
            return Forbid();
        }

        var studentExists = await _context.Students.AnyAsync(s => s.Id == id);
        if (!studentExists)
        {
            return NotFound();
        }

        var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId);
        if (!courseExists)
        {
            return BadRequest("Course not found");
        }

        var existingEnrollment = await _context.Enrollments.FindAsync(id, dto.CourseId);
        if (existingEnrollment != null)
        {
            return Conflict("Student is already enrolled in this course");
        }

        var enrollment = new Enrollment
        {
            StudentId = id,
            CourseId = dto.CourseId,
            Grade = null
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        var result = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == id && e.CourseId == dto.CourseId)
            .Select(e => new StudentEnrollmentDto
            {
                CourseId = e.CourseId,
                CourseCode = e.Course.Code,
                CourseTitle = e.Course.Title,
                Credits = e.Course.Credits,
                Grade = e.Grade
            })
            .SingleAsync();

        return Ok(result);
    }

    // PUT: api/students/{id}/courses/{courseId}
    // Admin can update grade for any student
    [HttpPut("{id:int}/courses/{courseId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StudentEnrollmentDto>> UpdateStudentEnrollment(
        int id,
        int courseId,
        [FromBody] StudentEnrollmentUpdateDto dto)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        var enrollment = await _context.Enrollments.FindAsync(id, courseId);
        if (enrollment == null)
        {
            return NotFound();
        }

        enrollment.Grade = dto.Grade;

        await _context.SaveChangesAsync();

        var result = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == id && e.CourseId == courseId)
            .Select(e => new StudentEnrollmentDto
            {
                CourseId = e.CourseId,
                CourseCode = e.Course.Code,
                CourseTitle = e.Course.Title,
                Credits = e.Course.Credits,
                Grade = e.Grade
            })
            .SingleAsync();

        return Ok(result);
    }

    // DELETE: api/students/{id}/courses/{courseId}
    // Admin can remove enrollment for any student, but Student can remove only own enrollment
    [HttpDelete("{id:int}/courses/{courseId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteStudentEnrollment(int id, int courseId)
    {
        if (!CanAccessStudent(id))
        {
            return Forbid();
        }

        var enrollment = await _context.Enrollments.FindAsync(id, courseId);
        if (enrollment == null)
        {
            return NotFound();
        }

        _context.Enrollments.Remove(enrollment);
        await _context.SaveChangesAsync();

        return NoContent();
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
