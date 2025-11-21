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

    // GET: api/students/ping
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "Students API is working" });
    }

    // GET: api/students
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
    {
        var students = await _context.Students
            .AsNoTracking() // For read-only operation to make request lighter for CPU, RAM
            .ToListAsync();

        return Ok(students);
    }

    // GET: api/students/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Student>> GetStudent(int id)
    {
        var student = await _context.Students
            .AsNoTracking()
            .Include(s => s.Department)
            .SingleOrDefaultAsync(s => s.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        return Ok(student);
    }
}
