using FluentValidation;

namespace SmartRail.Modules.Network.Application.Stations.GetStations;

public sealed class GetStationsQueryValidator : AbstractValidator<GetStationsQuery>
{
    public GetStationsQueryValidator()
    {
        // Empty is allowed (no filter). One character is too broad to be useful.
        RuleFor(x => x.Search)
            .Must(s => s is null || s.Trim().Length == 0 || s.Trim().Length >= 2)
            .WithMessage("A search needs at least 2 characters.");
    }
}
