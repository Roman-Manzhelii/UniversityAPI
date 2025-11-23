using UniversityAPI.Models;

namespace UniversityAPI.Services;

public interface IStudentService
{
    Task<IEnumerable<StudentSummaryDto>> GetStudentsAsync();
    Task<StudentDetailsDto?> GetStudentAsync(int id);
    Task<StudentDetailsDto> CreateStudentAsync(StudentCreateDto dto);
    Task<StudentDetailsDto?> UpdateStudentAsync(int id, StudentUpdateDto dto);

    Task<IEnumerable<StudentEnrollmentDto>> GetEnrollmentsAsync(int studentId);
    Task<StudentEnrollmentDto?> EnrollAsync(int studentId, StudentEnrollmentCreateDto dto);
    Task<StudentEnrollmentDto?> UpdateEnrollmentAsync(
        int studentId,
        int courseId,
        StudentEnrollmentUpdateDto dto);
    Task<bool> DeleteEnrollmentAsync(int studentId, int courseId);
}
