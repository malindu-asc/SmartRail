using SmartRail.Modules.Network.Domain.Routes;
using SmartRail.Modules.Network.Domain.Stations;

namespace SmartRail.Modules.Network.Infrastructure.Seeding;

// Fictional demo network (ASM-017). Values are rounded demo numbers.
internal static class NetworkSeedData
{
    internal const string DefaultLanguage = "en";
    internal const string SecondLanguage = "si";

    internal sealed record StationSeed(string Code, string Name, string Sinhala, string[] Facilities, string[] Platforms);

    internal sealed record StopSeed(string StationCode, int MinutesFromPrevious, int DwellMinutes, StopKind Kind, string? Platform);

    internal sealed record RouteSeed(string Code, string Name, StopSeed[] Stops);

    internal static readonly StationSeed[] Stations =
    [
        new("CEN", "Central", "මධ්‍යම", [FacilityCodes.StepFree, FacilityCodes.Lifts, FacilityCodes.Toilets, FacilityCodes.TicketOffice, FacilityCodes.AssistancePoint], ["1", "2", "3"]),
        new("OAK", "Oak Park", "ඕක් පාර්ක්", [FacilityCodes.Toilets], ["1", "2"]),
        new("MIL", "Mill Lane", "මිල් ලේන්", [], ["1"]),
        new("RIV", "Riverside", "රිවර්සයිඩ්", [FacilityCodes.StepFree, FacilityCodes.Toilets, FacilityCodes.TicketOffice], ["1", "2"]),
        new("HIL", "Hill Town", "හිල් නගරය", [FacilityCodes.StepFree, FacilityCodes.Toilets], ["1", "2"]),
        new("MAR", "Market Street", "වෙළඳ වීදිය", [FacilityCodes.Toilets, FacilityCodes.TicketOffice], ["1", "2", "3"]),
        new("HAR", "Harbour", "වරාය", [FacilityCodes.Toilets], ["1"]),
        new("LAK", "Lakeside", "විල අසල", [], ["1"]),
        new("BAY", "Bay View", "බේ වීව්", [FacilityCodes.Toilets], ["1", "2"]),
        new("NOR", "North Gate", "උතුරු දොරටුව", [], ["1"]),
        new("GRN", "Green Hill", "හරිත කන්ද", [], ["1"]),
        new("AIR", "Airport", "ගුවන් තොටුපළ", [FacilityCodes.StepFree, FacilityCodes.Lifts, FacilityCodes.Toilets, FacilityCodes.TicketOffice, FacilityCodes.AssistancePoint], ["1", "2"])
    ];

    internal static readonly RouteSeed[] Routes =
    [
        new("MAIN", "Central to Riverside",
        [
            new("CEN", 0, 2, StopKind.Stop, "2"),
            new("OAK", 6, 1, StopKind.Stop, "1"),
            new("MIL", 7, 1, StopKind.Stop, "1"),
            new("RIV", 8, 2, StopKind.Stop, "1")
        ]),
        new("HILL", "Central to Hill Town",
        [
            new("CEN", 0, 2, StopKind.Stop, "1"),
            new("OAK", 6, 1, StopKind.Stop, "2"),
            new("HIL", 9, 1, StopKind.Stop, "1"),
            new("RIV", 10, 1, StopKind.Stop, "2"),
            new("MAR", 6, 2, StopKind.Stop, "1")
        ]),
        new("COAST", "Central to Bay View",
        [
            new("CEN", 0, 2, StopKind.Stop, "3"),
            new("HAR", 8, 0, StopKind.Passing, null),
            new("LAK", 9, 1, StopKind.Stop, "1"),
            new("BAY", 7, 2, StopKind.Stop, "1")
        ]),
        new("NORTH", "Market Street to Airport",
        [
            new("MAR", 0, 2, StopKind.Stop, "3"),
            new("NOR", 5, 1, StopKind.Stop, "1"),
            new("GRN", 6, 1, StopKind.Stop, "1"),
            new("AIR", 9, 2, StopKind.Stop, "1")
        ])
    ];
}
