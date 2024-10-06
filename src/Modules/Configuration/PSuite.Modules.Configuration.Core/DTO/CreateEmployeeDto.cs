using FluentValidation;

namespace PSuite.Modules.Configuration.Core.DTO;

internal record class CreateEmployeeDto(Guid Id, string FirstName, string LastName, Guid? HotelId, string Email);

internal class CreateEmployeeDtoValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Email).EmailAddress();
    }
}