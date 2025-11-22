using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityAPI.Models;

namespace UniversityAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly UniversityContext _context;

    public CoursesController(UniversityContext context)
    {
        _context = context;
    }

    // GET: api/courses
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CourseSummaryDto>>> GetCourses()
    {
        var courses = await _context.Courses
            .AsNoTracking() // For read-only operation to make request lighter for CPU, RAM
            .Include(c => c.Department)
            .Select(c => new CourseSummaryDto
            {
                Id = c.Id,
                Code = c.Code,
                Title = c.Title,
                Credits = c.Credits,
                DepartmentId = c.DepartmentId,
                DepartmentName = c.Department.Name
            })
            .ToListAsync();

        return Ok(courses);
    }

    // GET: api/courses/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CourseDetailsDto>> GetCourse(int id)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseDetailsDto
            {
                Id = c.Id,
                Code = c.Code,
                Title = c.Title,
                Credits = c.Credits,
                Department = new DepartmentDto
                {
                    Id = c.Department.Id,
                    Name = c.Department.Name
                }
            })
            .SingleOrDefaultAsync();

        if (course == null)
        {
            return NotFound();
        }

        return Ok(course);
    }

    // POST: api/courses
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourseDetailsDto>> CreateCourse([FromBody] CourseCreateDto dto)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
        if (!departmentExists)
        {
            return BadRequest("Department not found");
        }

        var course = new Course
        {
            Code = dto.Code,
            Title = dto.Title,
            Credits = dto.Credits,
            DepartmentId = dto.DepartmentId
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var details = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == course.Id)
            .Select(c => new CourseDetailsDto
            {
                Id = c.Id,
                Code = c.Code,
                Title = c.Title,
                Credits = c.Credits,
                Department = new DepartmentDto
                {
                    Id = c.Department.Id,
                    Name = c.Department.Name
                }
            })
            .SingleOrDefaultAsync();

        if (details == null)
        {
            return StatusCode(500);
        }

        return CreatedAtAction(nameof(GetCourse), new { id = details.Id }, details);
    }

    // PUT: api/courses/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourseDetailsDto>> UpdateCourse(int id, [FromBody] CourseUpdateDto dto)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }

        var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
        if (!departmentExists)
        {
            return BadRequest("Department not found");
        }

        course.Code = dto.Code;
        course.Title = dto.Title;
        course.Credits = dto.Credits;
        course.DepartmentId = dto.DepartmentId;

        await _context.SaveChangesAsync();

        var details = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseDetailsDto
            {
                Id = c.Id,
                Code = c.Code,
                Title = c.Title,
                Credits = c.Credits,
                Department = new DepartmentDto
                {
                    Id = c.Department.Id,
                    Name = c.Department.Name
                }
            })
            .SingleOrDefaultAsync();

        if (details == null)
        {
            return StatusCode(500);
        }

        return Ok(details);
    }

    // DELETE: api/courses/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
