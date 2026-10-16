using System.Text.RegularExpressions;
using SmartRail.Modules.Network.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Domain.Routes;

// An ordered line. It is bidirectional; direction of travel belongs to the service (timetable spec).
public sealed partial class Route : AuditableEntity
{
    private readonly List<RouteStop> _stops = [];

    private Route() { }

    private Route(Guid id) : base(id) { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public IReadOnlyList<RouteStop> Stops => _stops;

    public static Result<Route> Create(string code, string name, IReadOnlyList<RouteStopDraft> stops, Guid? id = null)
    {
        if (!RouteCodePattern().IsMatch(code))
            return NetworkErrors.InvalidRouteCode(code);

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            return NetworkErrors.InvalidName;

        if (stops.Count < 2)
            return NetworkErrors.RouteNeedsTwoStops;

        for (var i = 0; i < stops.Count; i++)
        {
            if (stops[i].Sequence != i + 1)
                return NetworkErrors.RouteSequenceInvalid;
        }

        if (stops.Select(s => s.StationId).Distinct().Count() != stops.Count)
            return NetworkErrors.RouteStationRepeated;

        if (stops.Any(s => s.MinutesFromPrevious < 0 || s.DwellMinutes < 0))
            return NetworkErrors.RouteTimeNegative;

        if (stops[0].MinutesFromPrevious != 0)
            return NetworkErrors.RouteFirstStopTime;

        var route = new Route(id ?? Guid.NewGuid()) { Code = code, Name = name.Trim() };
        route._stops.AddRange(stops.Select(s => new RouteStop(route.Id, s)));
        return route;
    }

    [GeneratedRegex("^[A-Z]{2,10}$")]
    private static partial Regex RouteCodePattern();
}
