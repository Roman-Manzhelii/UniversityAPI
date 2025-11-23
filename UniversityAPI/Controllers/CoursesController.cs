using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityAPI.Models;
using UniversityAPI.Services;

namespace UniversityAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courses;

    public CoursesController(ICourseService courses)
    {
        _courses = courses;
    }

    // GET: api/courses
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CourseSummaryDto>>> GetCourses()
    {
        var courses = await _courses.GetCoursesAsync();
        return Ok(courses);
    }

    // GET: api/courses/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CourseDetailsDto>> GetCourse(int id)
    {
        var course = await _courses.GetCourseAsync(id);

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

        try
        {
            var created = await _courses.CreateCourseAsync(dto);
            return CreatedAtAction(nameof(GetCourse), new { id = created.Id }, created);
        }
        catch (ArgumentNullException)
        {
            return BadRequest();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Credits must be greater than 0")
        {
            return BadRequest("Credits must be greater than 0");
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

    // PUT: api/courses/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourseDetailsDto>> UpdateCourse(int id, [FromBody] CourseUpdateDto dto)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        try
        {
            var updated = await _courses.UpdateCourseAsync(id, dto);

            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }
        catch (ArgumentNullException)
        {
            return BadRequest();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Credits must be greater than 0")
        {
            return BadRequest("Credits must be greater than 0");
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

    // DELETE: api/courses/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var deleted = await _courses.DeleteCourseAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
