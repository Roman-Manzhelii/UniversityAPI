using Microsoft.EntityFrameworkCore;
using UniversityAPI.Models;

namespace UniversityAPI.Services;

public class DepartmentService : IDepartmentService
{
    private readonly UniversityContext _context;

    public DepartmentService(UniversityContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DepartmentDto>> GetDepartmentsAsync()
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Id)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name
            })
            .ToListAsync();
    }

    public async Task<DepartmentDto?> GetDepartmentAsync(int id)
    {
        return await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name
            })
            .SingleOrDefaultAsync();
    }
}
