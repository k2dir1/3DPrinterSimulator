// Application/DTOs/PrinterDto.cs
namespace _3DPrinterSimulator.Application.DTOs;

public record PrinterDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Brand { get; init; } = default!;
    public string Model { get; init; } = default!;
    public string PrinterType { get; init; } = default!;
    public string Status { get; init; } = default!;
    public double NozzleTemp { get; init; }
    public double TargetNozzleTemp { get; init; }
    public double BedTemp { get; init; }
    public double TargetBedTemp { get; init; }
    public int Progress { get; init; }
    public double FeedRate { get; init; }
    public double FilamentRemainingGrams { get; init; }
    public bool HasFilamentWarning { get; init; }
    public bool HasFilamentRunout { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CurrentJobName { get; init; }
    public double? EstimatedRemainingHours { get; init; }
    public IEnumerable<string> PermittedTriggers { get; init; } = [];
}