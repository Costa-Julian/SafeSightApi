using FluentValidation;
using SafeSight.Api.Models.Dtos.Requests;

namespace SafeSight.Api.Validators;

public class CreateAlertRequestValidator : AbstractValidator<CreateAlertRequest>
{
    public CreateAlertRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100)
            .WithMessage("El nombre es obligatorio y no puede superar 100 caracteres.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100)
            .WithMessage("El apellido es obligatorio y no puede superar 100 caracteres.");
        RuleFor(x => x.Age).InclusiveBetween(0, 120)
            .WithMessage("La edad debe estar entre 0 y 120.");
        RuleFor(x => x.PhysicalDescription).NotEmpty().MaximumLength(500)
            .WithMessage("La descripción física es obligatoria.");
        RuleFor(x => x.Situation).NotEmpty().MaximumLength(1000)
            .WithMessage("La situación es obligatoria.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90)
            .WithMessage("La latitud debe estar entre -90 y 90.");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180)
            .WithMessage("La longitud debe estar entre -180 y 180.");
        RuleFor(x => x.DisappearanceDate).LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("La fecha de desaparición no puede ser futura.");
        RuleFor(x => x.EmitterId).InclusiveBetween(1, 2)
            .WithMessage("El identificador de emisor debe ser 1 (ciudadano) o 2 (entidad).");
    }
}
