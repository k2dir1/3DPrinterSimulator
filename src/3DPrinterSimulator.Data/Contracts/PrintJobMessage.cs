namespace _3DPrinterSimulator.Application.Contracts;

public record PrintJobMessage
{
    public Guid JobId { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = default!;
    public double Duration { get; init; }
    public double FilamentGrams { get; init; }
    public string TargetPrinterType { get; init; } = "any";
    public byte Priority { get; init; } = 0;
    public int RetryCount { get; init; } = 0;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}