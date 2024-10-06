using FluentValidation;

namespace PSuite.Modules.Configuration.Core.DTO;

internal record EmployeeDto(Guid Id, string FirstName, string LastName, Guid? HotelId);

internal class EmployeeDtoValidator : AbstractValidator<EmployeeDto>
{
    public EmployeeDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}