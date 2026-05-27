using FluentValidation;
using SafeSight.Api.Models.Dtos.Requests;

namespace SafeSight.Api.Validators;

public class AwarenessReportRequestValidator : AbstractValidator<AwarenessReportRequest>
{
    public AwarenessReportRequestValidator()
    {
        RuleFor(x => x.AlertId).NotEmpty()
            .WithMessage("El identificador de alerta es obligatorio.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90)
            .WithMessage("La latitud debe estar entre -90 y 90.");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180)
            .WithMessage("La longitud debe estar entre -180 y 180.");
        RuleFor(x => x.ReportedAt).NotEmpty()
            .WithMessage("La fecha del reporte es obligatoria.");
    }
}
