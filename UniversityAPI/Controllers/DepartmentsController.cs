using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityAPI.Models;
using UniversityAPI.Services;

namespace UniversityAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departments;

    public DepartmentsController(IDepartmentService departments)
    {
        _departments = departments;
    }

    // GET: api/departments
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
    {
        var departments = await _departments.GetDepartmentsAsync();
        return Ok(departments);
    }

    // GET: api/departments/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<DepartmentDto>> GetDepartment(int id)
    {
        var department = await _departments.GetDepartmentAsync(id);
        if (department == null)
        {
            return NotFound();
        }

        return Ok(department);
    }
}
