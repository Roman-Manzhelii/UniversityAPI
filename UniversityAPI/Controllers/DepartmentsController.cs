using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityAPI.Models;

namespace UniversityAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly UniversityContext _context;

    public DepartmentsController(UniversityContext context)
    {
        _context = context;
    }

    // GET: api/departments
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name
            })
            .ToListAsync();

        return Ok(departments);
    }

    // GET: api/departments/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<DepartmentDto>> GetDepartment(int id)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name
            })
            .SingleOrDefaultAsync();

        if (department == null)
        {
            return NotFound();
        }

        return Ok(department);
    }
}
