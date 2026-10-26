namespace CivicVoice.Domain.Events;

/// <summary>
/// Domain event raised when a grievance is resolved.
/// Used to initiate the feedback workflow — the citizen becomes eligible to rate the resolution.
/// Demonstrates meaningful use of delegates/events in the application.
/// </summary>
public class GrievanceResolvedEventArgs : EventArgs
{
    public int GrievanceId { get; }
    public string CitizenId { get; }
    public DateTime ResolvedAt { get; }

    public GrievanceResolvedEventArgs(int grievanceId, string citizenId, DateTime resolvedAt)
    {
        GrievanceId = grievanceId;
        CitizenId = citizenId;
        ResolvedAt = resolvedAt;
    }
}

/// <summary>
/// Delegate for grievance resolution events.
/// </summary>
public delegate void GrievanceResolvedHandler(object sender, GrievanceResolvedEventArgs e);
