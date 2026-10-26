namespace CivicVoice.Domain.Enums;

/// <summary>
/// Defines the lifecycle states of a grievance.
/// Transitions follow a state machine:
///   Submitted → Assigned → InProgress → Resolved
///   Submitted → Rejected
///   Assigned → Rejected
///   InProgress → Rejected
/// </summary>
public enum GrievanceStatus
{
    Submitted = 0,
    Assigned = 1,
    InProgress = 2,
    Resolved = 3,
    Rejected = 4
}
