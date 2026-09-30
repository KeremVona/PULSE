namespace HeartCDSS.Domain.Common;

/// <summary>
/// Marker interface for all domain events emitted by aggregate roots.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier of the specific domain event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// UTC timestamp of when the event occurred in the domain.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
