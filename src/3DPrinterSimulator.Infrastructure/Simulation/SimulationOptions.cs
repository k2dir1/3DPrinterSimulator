namespace _3DPrinterSimulator.Infrastructure.Simulation;

public record SimulationOptions
{
    public const string SectionName = "Simulation";

    public int TickIntervalMs { get; init; } = 5_000;

    public double AutoJobProbability { get; init; } = 0;

    public double FilamentWarningThresholdGrams { get; init; } = 120.0;
}
