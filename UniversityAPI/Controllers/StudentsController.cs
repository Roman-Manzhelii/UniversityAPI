using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityAPI.Models;
using UniversityAPI.Services;

namespace UniversityAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _students;

    public StudentsController(IStudentService students)
    {
        _students = students;
    }

    // GET: api/students
    // Admin: list all students with basic info
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<StudentSummaryDto>>> GetStudents()
    {
        var students = await _students.GetStudentsAsync();
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
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StudentDetailsDto>> CreateStudent([FromBody] StudentCreateDto dto)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        try
        {
            var details = await _students.CreateStudentAsync(dto);
            return CreatedAtAction(nameof(GetStudent), new { id = details.Id }, details);
        }
        catch (ArgumentNullException)
        {
            return BadRequest();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Department not found")
        {
            return BadRequest("Department not found");
        }
        catch (InvalidOperationException)
        {
            return StatusCode(500);
        }
    }

    // PUT: api/students/{id}
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

        try
        {
            var details = await _students.UpdateStudentAsync(id, dto);
            if (details == null)
            {
                return NotFound();
            }

            return Ok(details);
        }
        catch (ArgumentNullException)
        {
            return BadRequest();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Department not found")
        {
            return BadRequest("Department not found");
        }
        catch (InvalidOperationException)
        {
            return StatusCode(500);
        }
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

        var enrollments = await _students.GetEnrollmentsAsync(id);

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

        try
        {
            var result = await _students.EnrollAsync(id, dto);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (ArgumentNullException)
        {
            return BadRequest();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Course not found")
        {
            return BadRequest("Course not found");
        }
        catch (InvalidOperationException ex) when (ex.Message == "Student is already enrolled in this course")
        {
            return Conflict("Student is already enrolled in this course");
        }
        catch (InvalidOperationException)
        {
            return StatusCode(500);
        }
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

        try
        {
            var result = await _students.UpdateEnrollmentAsync(id, courseId, dto);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (ArgumentNullException)
        {
            return BadRequest();
        }
        catch (InvalidOperationException)
        {
            return StatusCode(500);
        }
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

        var deleted = await _students.DeleteEnrollmentAsync(id, courseId);
        if (!deleted)
        {
            return NotFound();
        }

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
    private Task<StudentDetailsDto?> FindStudentDetailsAsync(int id)
    {
        return _students.GetStudentAsync(id);
    }
}
