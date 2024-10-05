using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Services;

namespace PSuite.Modules.Configuration.Api.Endpoints;

internal static class EmployeeApi
{
    internal static void RegisterEmployeeApi(this IEndpointRouteBuilder app, string basePath)
    {
        var employeeEndpoints = app.MapGroup($"{basePath}/employees")
            .WithTags("Employee")
            .WithOpenApi()
            .WithMetadata()
            .RequireAuthorization();

        employeeEndpoints.MapPost("", (IEmployeeService employeeService, CreateEmployeeDto request) => employeeService.CreateAsync(request))
            .WithName("Create employee");
        
        employeeEndpoints.MapPut("/{id:guid}", (IEmployeeService employeeService, Guid id, EmployeeDto request) => employeeService.UpdateAsync(request))
            .WithName("Update employee");

        employeeEndpoints.MapDelete("/{id:guid}", (IEmployeeService employeeService, Guid id) => employeeService.DeleteAsync(id))
            .WithName("Delete employee");

        employeeEndpoints.MapGet("", (IEmployeeService employeeService) => employeeService.GetAllAsync())
            .WithName("Get all employee");

        employeeEndpoints.MapGet("/{id:guid}", (IEmployeeService employeeService, Guid id) => employeeService.GetByIdAsync(id))
            .WithName("Get employee by id");
    }
}
