using Microsoft.EntityFrameworkCore;
using UniversityAPI.Models;

namespace UniversityAPI.Services;

public class StudentService : IStudentService
{
    private readonly UniversityContext _context;

    public StudentService(UniversityContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StudentSummaryDto>> GetStudentsAsync()
    {
        return await _context.Students
            .AsNoTracking()
            .Include(s => s.Department)
            .OrderBy(s => s.Id)
            .Select(s => new StudentSummaryDto
            {
                Id = s.Id,
                StudentNumber = s.StudentNumber,
                Name = s.Name,
                TotalCredits = s.TotalCredits,
                DepartmentId = s.DepartmentId,
                DepartmentName = s.Department != null ? s.Department.Name : null
            })
            .ToListAsync();
    }

    public async Task<StudentDetailsDto?> GetStudentAsync(int id)
    {
        return await _context.Students
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StudentDetailsDto
            {
                Id = s.Id,
                StudentNumber = s.StudentNumber,
                Name = s.Name,
                TotalCredits = s.TotalCredits,
                Department = s.Department == null
                    ? null
                    : new DepartmentDto
                    {
                        Id = s.Department.Id,
                        Name = s.Department.Name
                    },
                Enrollments = s.Enrollments
                    .Select(e => new StudentEnrollmentDto
                    {
                        CourseId = e.CourseId,
                        CourseCode = e.Course.Code,
                        CourseTitle = e.Course.Title,
                        Credits = e.Course.Credits,
                        Grade = e.Grade
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync();
    }

    public async Task<StudentDetailsDto> CreateStudentAsync(StudentCreateDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (dto.DepartmentId.HasValue)
        {
            var departmentExists = await _context.Departments
                .AnyAsync(d => d.Id == dto.DepartmentId.Value);

            if (!departmentExists)
            {
                throw new InvalidOperationException("Department not found");
            }
        }

        var student = new Student
        {
            StudentNumber = dto.StudentNumber,
            Name = dto.Name,
            TotalCredits = 0,
            DepartmentId = dto.DepartmentId
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var created = await GetStudentAsync(student.Id);
        if (created == null)
        {
            throw new InvalidOperationException("Created student could not be loaded");
        }

        return created;
    }

    public async Task<StudentDetailsDto?> UpdateStudentAsync(int id, StudentUpdateDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var student = await _context.Students.FindAsync(id);
        if (student == null)
        {
            return null;
        }

        if (dto.DepartmentId.HasValue)
        {
            var departmentExists = await _context.Departments
                .AnyAsync(d => d.Id == dto.DepartmentId.Value);

            if (!departmentExists)
            {
                throw new InvalidOperationException("Department not found");
            }
        }

        student.Name = dto.Name;
        student.DepartmentId = dto.DepartmentId;

        await _context.SaveChangesAsync();

        var updated = await GetStudentAsync(id);
        if (updated == null)
        {
            throw new InvalidOperationException("Updated student could not be loaded");
        }

        return updated;
    }

    public async Task<IEnumerable<StudentEnrollmentDto>> GetEnrollmentsAsync(int studentId)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => new StudentEnrollmentDto
            {
                CourseId = e.CourseId,
                CourseCode = e.Course.Code,
                CourseTitle = e.Course.Title,
                Credits = e.Course.Credits,
                Grade = e.Grade
            })
            .ToListAsync();
    }

    public async Task<StudentEnrollmentDto?> EnrollAsync(int studentId, StudentEnrollmentCreateDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var studentExists = await _context.Students.AnyAsync(s => s.Id == studentId);
        if (!studentExists)
        {
            return null;
        }

        var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId);
        if (!courseExists)
        {
            throw new InvalidOperationException("Course not found");
        }

        var existing = await _context.Enrollments.FindAsync(studentId, dto.CourseId);
        if (existing != null)
        {
            throw new InvalidOperationException("Student is already enrolled in this course");
        }

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            Grade = null
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        var result = await MapEnrollmentAsync(studentId, dto.CourseId);
        if (result == null)
        {
            throw new InvalidOperationException("Enrollment could not be loaded");
        }

        return result;
    }

    public async Task<StudentEnrollmentDto?> UpdateEnrollmentAsync(
        int studentId,
        int courseId,
        StudentEnrollmentUpdateDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var enrollment = await _context.Enrollments.FindAsync(studentId, courseId);
        if (enrollment == null)
        {
            return null;
        }

        enrollment.Grade = dto.Grade;
        await _context.SaveChangesAsync();

        var result = await MapEnrollmentAsync(studentId, courseId);
        if (result == null)
        {
            throw new InvalidOperationException("Enrollment could not be loaded");
        }

        return result;
    }

    public async Task<bool> DeleteEnrollmentAsync(int studentId, int courseId)
    {
        var enrollment = await _context.Enrollments.FindAsync(studentId, courseId);
        if (enrollment == null)
        {
            return false;
        }

        _context.Enrollments.Remove(enrollment);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<StudentEnrollmentDto?> MapEnrollmentAsync(int studentId, int courseId)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId && e.CourseId == courseId)
            .Select(e => new StudentEnrollmentDto
            {
                CourseId = e.CourseId,
                CourseCode = e.Course.Code,
                CourseTitle = e.Course.Title,
                Credits = e.Course.Credits,
                Grade = e.Grade
            })
            .SingleOrDefaultAsync();
    }
}
