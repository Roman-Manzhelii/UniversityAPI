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
}
