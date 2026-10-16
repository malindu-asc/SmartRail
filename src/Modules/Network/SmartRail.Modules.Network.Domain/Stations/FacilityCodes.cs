namespace SmartRail.Modules.Network.Domain.Stations;

// Static facility codes. Catering is not here: the catering provider owns that data (REQ-INT-001).
public static class FacilityCodes
{
    public const string StepFree = "StepFree";
    public const string Lifts = "Lifts";
    public const string Toilets = "Toilets";
    public const string TicketOffice = "TicketOffice";
    public const string AssistancePoint = "AssistancePoint";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        StepFree, Lifts, Toilets, TicketOffice, AssistancePoint
    };
}
