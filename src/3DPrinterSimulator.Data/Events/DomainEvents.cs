namespace _3DPrinterSimulator.Data.Events;

public interface IDomainEvent
{
    Guid PrinterId { get; }
    DateTime OccurredAt { get; }
}

public record JobAssignedEvent(Guid PrinterId, Guid JobId, DateTime OccurredAt) : IDomainEvent;

public record TempReachedEvent(Guid PrinterId, double Temperature, DateTime OccurredAt) : IDomainEvent;

public record FilamentRunoutEvent(Guid PrinterId, DateTime OccurredAt) : IDomainEvent;

public record CriticalTempEvent(Guid PrinterId, double Temperature, DateTime OccurredAt) : IDomainEvent;

public record PrintCompletedEvent(Guid PrinterId, Guid JobId, DateTime OccurredAt) : IDomainEvent;
