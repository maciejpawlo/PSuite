using FluentValidation;

namespace PSuite.Modules.Configuration.Core.DTO;

internal record RoomDto(Guid Id, int Capacity, string Number, Guid HotelId);

internal class RoomDtoValidator : AbstractValidator<RoomDto>
{
    public RoomDtoValidator()
    {
        RuleFor(x => x.Capacity).GreaterThan(0);
        RuleFor(x => x.Number).NotEmpty();
        RuleFor(x => x.HotelId).NotEqual(Guid.Empty);
    }
}