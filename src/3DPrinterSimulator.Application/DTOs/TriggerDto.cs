namespace _3DPrinterSimulator.Application.DTOs
{
    public record TriggerDto(string Trigger, string? Name,
        double? EstimatedDurationHours, double? FilamentRequiredGrams);
}
