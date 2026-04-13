namespace _3DPrinterSimulator.Application.Contracts;

public record PrintJobRequest
{
    public Guid JobId { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = default!;
    public double Duration { get; init; }
    public double FilamentGrams { get; init; }
}