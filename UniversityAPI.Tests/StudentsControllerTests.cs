using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityAPI.Controllers;
using UniversityAPI.Models;

namespace UniversityAPI.Tests;

[TestFixture]
public class StudentsControllerTests
{
    private UniversityContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<UniversityContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new UniversityContext(options);

        SeedData(context);

        return context;
    }

    // Seed minimal data for departments, courses, students and enrollments
    private void SeedData(UniversityContext context)
    {
        var csDepartment = new Department
        {
            Id = 2,
            Name = "Comp. Sci.",
            Building = "Taylor",
            Budget = 100000
        };

        var csIntro = new Course
        {
            Id = 4,
            Code = "CS-101",
            Title = "Intro. to Computer Science",
            Credits = 4,
            DepartmentId = 2
        };

        var csGame = new Course
        {
            Id = 5,
            Code = "CS-190",
            Title = "Game Design",
            Credits = 4,
            DepartmentId = 2
        };

        var student1 = new Student
        {
            Id = 1,
            StudentNumber = "00128",
            Name = "Zhang",
            TotalCredits = 102,
            DepartmentId = 2
        };

        var student2 = new Student
        {
            Id = 2,
            StudentNumber = "12345",
            Name = "Shankar",
            TotalCredits = 32,
            DepartmentId = 2
        };

        var enrollment1 = new Enrollment
        {
            StudentId = 1,
            CourseId = 4,
            Grade = "A"
        };

        context.Departments.Add(csDepartment);
        context.Courses.AddRange(csIntro, csGame);
        context.Students.AddRange(student1, student2);
        context.Enrollments.Add(enrollment1);

        context.SaveChanges();
    }

    // Creates a controller instance and assigns a fake HttpContext with a given user
    private StudentsController CreateControllerWithUser(UniversityContext context, ClaimsPrincipal user)
    {
        var controller = new StudentsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };
        return controller;
    }

    // Admin user can access any student
    private ClaimsPrincipal CreateAdminPrincipal()
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Role, "Admin")
            },
            "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    // Student user has Role = Student and custom studentId claim
    private ClaimsPrincipal CreateStudentPrincipal(int studentId)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Role, "Student"),
                new Claim("studentId", studentId.ToString())
            },
            "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    [Test]
    public async Task GetStudent_AsAdmin_ReturnsStudentDetailsWithEnrollments()
    {
        using var context = CreateInMemoryContext();
        var admin = CreateAdminPrincipal();
        var controller = CreateControllerWithUser(context, admin);

        // Admin can access any student id
        var result = await controller.GetStudent(1);

        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult, "Expected OkObjectResult");

        var student = okResult!.Value as StudentDetailsDto;
        Assert.IsNotNull(student, "Expected StudentDetailsDto");

        Assert.That(student!.Id, Is.EqualTo(1));
        Assert.That(student.Name, Is.EqualTo("Zhang"));
        Assert.IsNotNull(student.Department);
        Assert.That(student.Department!.Name, Is.EqualTo("Comp. Sci."));

        // One enrollment for CS-101 with grade A
        Assert.That(student.Enrollments.Count, Is.EqualTo(1));
        var enrollment = student.Enrollments.Single();
        Assert.That(enrollment.CourseId, Is.EqualTo(4));
        Assert.That(enrollment.CourseCode, Is.EqualTo("CS-101"));
        Assert.That(enrollment.Grade, Is.EqualTo("A"));
    }

    [Test]
    public async Task GetStudent_AsAnotherStudent_ReturnsForbid()
    {
        using var context = CreateInMemoryContext();

        // Current user is student with studentId = 2
        var otherStudent = CreateStudentPrincipal(2);
        var controller = CreateControllerWithUser(context, otherStudent);

        // Trying to access student with id = 1 should be forbidden
        var result = await controller.GetStudent(1);

        Assert.IsInstanceOf<ForbidResult>(result.Result);
    }

    [Test]
    public async Task GetStudentCourses_AsStudent_ReturnsOwnCourses()
    {
        using var context = CreateInMemoryContext();

        // Student with studentId = 1
        var studentUser = CreateStudentPrincipal(1);
        var controller = CreateControllerWithUser(context, studentUser);

        var result = await controller.GetStudentCourses(1);

        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult, "Expected OkObjectResult");

        var courses = okResult!.Value as IEnumerable<StudentEnrollmentDto>;
        Assert.IsNotNull(courses, "Expected IEnumerable<StudentEnrollmentDto>");

        var list = courses!.ToList();
        Assert.That(list.Count, Is.EqualTo(1));

        var enrollment = list.Single();
        Assert.That(enrollment.CourseId, Is.EqualTo(4));
        Assert.That(enrollment.CourseCode, Is.EqualTo("CS-101"));
        Assert.That(enrollment.Grade, Is.EqualTo("A"));
    }

    [Test]
    public async Task EnrollStudentInCourse_AsStudent_CreatesNewEnrollment()
    {
        using var context = CreateInMemoryContext();

        // Student 1 can enroll only themselves
        var studentUser = CreateStudentPrincipal(1);
        var controller = CreateControllerWithUser(context, studentUser);

        var dto = new StudentEnrollmentCreateDto
        {
            CourseId = 5
        };

        var result = await controller.EnrollStudentInCourse(1, dto);

        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult, "Expected OkObjectResult");

        var enrollmentDto = okResult!.Value as StudentEnrollmentDto;
        Assert.IsNotNull(enrollmentDto, "Expected StudentEnrollmentDto");

        // Check returned DTO
        Assert.That(enrollmentDto!.CourseId, Is.EqualTo(5));
        Assert.That(enrollmentDto.CourseCode, Is.EqualTo("CS-190"));
        Assert.That(enrollmentDto.CourseTitle, Is.EqualTo("Game Design"));
        Assert.IsNull(enrollmentDto.Grade);

        // Check that enrollment exists in the in-memory database
        var enrollmentInDb = await context.Enrollments.FindAsync(1, 5);
        Assert.IsNotNull(enrollmentInDb);
    }

    [Test]
    public async Task DeleteStudentEnrollment_AsStudent_RemovesEnrollment()
    {
        using var context = CreateInMemoryContext();

        // Student 1 has an existing enrollment for course 4
        var studentUser = CreateStudentPrincipal(1);
        var controller = CreateControllerWithUser(context, studentUser);

        var result = await controller.DeleteStudentEnrollment(1, 4);

        // Expect HTTP 204 NoContent on successful delete
        Assert.IsInstanceOf<NoContentResult>(result);

        // Enrollment should no longer exist in the in-memory database
        var enrollmentInDb = await context.Enrollments.FindAsync(1, 4);
        Assert.IsNull(enrollmentInDb);
    }
}
