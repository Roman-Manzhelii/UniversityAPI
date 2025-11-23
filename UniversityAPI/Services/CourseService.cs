using Microsoft.EntityFrameworkCore;
using UniversityAPI.Models;

namespace UniversityAPI.Services;

public class CourseService : ICourseService
{
    private readonly UniversityContext _context;

    public CourseService(UniversityContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CourseSummaryDto>> GetCoursesAsync()
    {
        return await _context.Courses
            .AsNoTracking()
            .Include(c => c.Department)
            .OrderBy(c => c.Code)
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
    }

    public async Task<CourseDetailsDto?> GetCourseAsync(int id)
    {
        return await _context.Courses
            .AsNoTracking()
            .Include(c => c.Department)
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
    }

    public async Task<CourseDetailsDto> CreateCourseAsync(CourseCreateDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (dto.Credits <= 0)
        {
            throw new InvalidOperationException("Credits must be greater than 0");
        }

        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == dto.DepartmentId);

        if (!departmentExists)
        {
            throw new InvalidOperationException("Department not found");
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

        var created = await GetCourseAsync(course.Id);
        if (created == null)
        {
            throw new InvalidOperationException("Created course could not be loaded");
        }

        return created;
    }

    public async Task<CourseDetailsDto?> UpdateCourseAsync(int id, CourseUpdateDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (dto.Credits <= 0)
        {
            throw new InvalidOperationException("Credits must be greater than 0");
        }

        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return null;
        }

        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == dto.DepartmentId);

        if (!departmentExists)
        {
            throw new InvalidOperationException("Department not found");
        }

        course.Code = dto.Code;
        course.Title = dto.Title;
        course.Credits = dto.Credits;
        course.DepartmentId = dto.DepartmentId;

        await _context.SaveChangesAsync();

        return await GetCourseAsync(id);
    }

    public async Task<bool> DeleteCourseAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return false;
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return true;
    }
}
