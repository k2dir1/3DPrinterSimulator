namespace _3DPrinterSimulator.Application.DTOs;

public record AssignJobDto(
    string Name,
    double EstimatedDurationHours,
    double FilamentRequiredGrams
);