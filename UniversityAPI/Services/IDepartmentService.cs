using UniversityAPI.Models;

namespace UniversityAPI.Services;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentDto>> GetDepartmentsAsync();
    Task<DepartmentDto?> GetDepartmentAsync(int id);
}
