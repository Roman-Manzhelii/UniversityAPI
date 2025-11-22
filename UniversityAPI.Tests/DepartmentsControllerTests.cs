using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityAPI.Controllers;
using UniversityAPI.Models;

namespace UniversityAPI.Tests;

[TestFixture]
public class DepartmentsControllerTests
{
    // Creates a new in-memory DbContext with its own database per test
    private UniversityContext CreateInMemoryContext()
    {
        // Build DbContext options that tell EF Core to use an in-memory database instead of SQLite
        var options = new DbContextOptionsBuilder<UniversityContext>()
            // Guid.NewGuid().ToString() creates a unique database name for each test run
            // so every test gets a fresh
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create a new UniversityContext instance using these options
        var context = new UniversityContext(options);

        // Insert test data
        SeedDepartments(context);

        return context;
    }

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

    [Test]
    public async Task GetDepartments_ReturnsAllDepartments()
    {
        using var context = CreateInMemoryContext();

        // Create controller and inject test context instead of real database
        var controller = new DepartmentsController(context);

        // Action call
        var result = await controller.GetDepartments();

        // The action returns ActionResult<IEnumerable<DepartmentDto>>,
        // so we first extract the OkObjectResult wrapper
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult, "Expected OkObjectResult");

        // From OkObjectResult we get the actual value which should be a collection of DepartmentDto
        var departments = okResult!.Value as IEnumerable<DepartmentDto>;
        Assert.IsNotNull(departments, "Expected IEnumerable<DepartmentDto>");

        // Materialize to list so we can do simple checks
        var list = departments!.ToList();

        Assert.That(list.Count, Is.EqualTo(2));

        Assert.That(list.Any(d => d.Id == 1 && d.Name == "Biology"), Is.True);
        Assert.That(list.Any(d => d.Id == 2 && d.Name == "Comp. Sci."), Is.True);
    }


    [Test]
    public async Task GetDepartment_ExistingId_ReturnsDepartment()
    {
        using var context = CreateInMemoryContext();

        var controller = new DepartmentsController(context);

        var result = await controller.GetDepartment(2);

        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult, "Expected OkObjectResult");

        var department = okResult!.Value as DepartmentDto;
        Assert.IsNotNull(department, "Expected DepartmentDto");

        Assert.That(department!.Id, Is.EqualTo(2));
        Assert.That(department.Name, Is.EqualTo("Comp. Sci."));
    }

    [Test]
    public async Task GetDepartment_UnknownId_ReturnsNotFound()
    {
        using var context = CreateInMemoryContext();

        var controller = new DepartmentsController(context);

        var result = await controller.GetDepartment(999);

        // For unknown id we expect HTTP 404 NotFound
        Assert.IsInstanceOf<NotFoundResult>(result.Result);
    }
}
