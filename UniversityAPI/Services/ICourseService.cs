using UniversityAPI.Models;

namespace UniversityAPI.Services;

public interface ICourseService
{
    Task<IEnumerable<CourseSummaryDto>> GetCoursesAsync();
    Task<CourseDetailsDto?> GetCourseAsync(int id);
    Task<CourseDetailsDto> CreateCourseAsync(CourseCreateDto dto);
    Task<CourseDetailsDto?> UpdateCourseAsync(int id, CourseUpdateDto dto);
    Task<bool> DeleteCourseAsync(int id);
}
