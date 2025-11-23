using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityAPI.Controllers;
using UniversityAPI.Models;
using UniversityAPI.Services;

namespace UniversityAPI.Tests;

[TestFixture]
public class CoursesControllerTests
{
    private UniversityContext CreateInMemoryContext()
    {
        // Use a unique in-memory database name so each test gets a fresh database
        var options = new DbContextOptionsBuilder<UniversityContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new UniversityContext(options);

        SeedDepartments(context);
        SeedCourses(context);

        return context;
    }

    // Seed minimal Departments so foreign keys for courses are valid
    private void SeedDepartments(UniversityContext context)
    {
        context.Departments.AddRange(
            new Department
            {
                Id = 1,
                Name = "Biology",
                Building = "Watson",
                Budget = 90000
            },
            new Department
            {
                Id = 2,
                Name = "Comp. Sci.",
                Building = "Taylor",
                Budget = 100000
            }
        );

        context.SaveChanges();
    }

    // Seed a few Courses that belong to the seeded Departments
    private void SeedCourses(UniversityContext context)
    {
        context.Courses.AddRange(
            new Course
            {
                Id = 4,
                Code = "CS-101",
                Title = "Intro. to Computer Science",
                Credits = 4,
                DepartmentId = 2
            },
            new Course
            {
                Id = 5,
                Code = "CS-190",
                Title = "Game Design",
                Credits = 4,
                DepartmentId = 2
            }
        );

        context.SaveChanges();
    }

    [Test]
    public async Task GetCourses_ReturnsAllCourses()
    {
        using var context = CreateInMemoryContext();
        var service = new CourseService(context);
        var controller = new CoursesController(service);

        var result = await controller.GetCourses();

        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult, "Expected OkObjectResult");

        var courses = okResult!.Value as IEnumerable<CourseSummaryDto>;
        Assert.IsNotNull(courses, "Expected IEnumerable<CourseSummaryDto>");

        var list = courses!.ToList();

        Assert.That(list.Count, Is.EqualTo(2));

        Assert.That(
            list.Any(c =>
                c.Id == 4 &&
                c.Code == "CS-101" &&
                c.Title == "Intro. to Computer Science" &&
                c.DepartmentId == 2 &&
                c.DepartmentName == "Comp. Sci."),
            Is.True);

        Assert.That(
            list.Any(c =>
                c.Id == 5 &&
                c.Code == "CS-190" &&
                c.Title == "Game Design" &&
                c.DepartmentId == 2 &&
                c.DepartmentName == "Comp. Sci."),
            Is.True);
    }

    [Test]
    public async Task GetCourse_ExistingId_ReturnsCourse()
    {
        using var context = CreateInMemoryContext();
        var service = new CourseService(context);
        var controller = new CoursesController(service);

        var result = await controller.GetCourse(4);

        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult, "Expected OkObjectResult");

        var course = okResult!.Value as CourseDetailsDto;
        Assert.IsNotNull(course, "Expected CourseDetailsDto");

        Assert.That(course!.Id, Is.EqualTo(4));
        Assert.That(course.Code, Is.EqualTo("CS-101"));
        Assert.That(course.Title, Is.EqualTo("Intro. to Computer Science"));
        Assert.That(course.Credits, Is.EqualTo(4));
        Assert.IsNotNull(course.Department);
        Assert.That(course.Department!.Id, Is.EqualTo(2));
        Assert.That(course.Department.Name, Is.EqualTo("Comp. Sci."));
    }

    [Test]
    public async Task GetCourse_UnknownId_ReturnsNotFound()
    {
        using var context = CreateInMemoryContext();
        var service = new CourseService(context);
        var controller = new CoursesController(service);

        var result = await controller.GetCourse(999);

        Assert.IsInstanceOf<NotFoundResult>(result.Result);
    }

    [Test]
    public async Task CreateCourse_ValidData_ReturnsCreatedCourse()
    {
        using var context = CreateInMemoryContext();
        var service = new CourseService(context);
        var controller = new CoursesController(service);

        var dto = new CourseCreateDto
        {
            Code = "CS-999",
            Title = "Special Topics",
            Credits = 3,
            DepartmentId = 2
        };

        var result = await controller.CreateCourse(dto);

        var createdResult = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(createdResult, "Expected CreatedAtActionResult");

        var course = createdResult!.Value as CourseDetailsDto;
        Assert.IsNotNull(course, "Expected CourseDetailsDto");

        Assert.That(course!.Id, Is.GreaterThan(0));
        Assert.That(course.Code, Is.EqualTo("CS-999"));
        Assert.That(course.Title, Is.EqualTo("Special Topics"));
        Assert.That(course.Credits, Is.EqualTo(3));
        Assert.IsNotNull(course.Department);
        Assert.That(course.Department!.Id, Is.EqualTo(2));
        Assert.That(course.Department.Name, Is.EqualTo("Comp. Sci."));
    }
}
