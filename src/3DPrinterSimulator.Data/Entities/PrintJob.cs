namespace _3DPrinterSimulator.Data.Entities;

public class PrintJob
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; }

    public double EstimatedDurationHours { get; private set; }

    public double FilamentRequiredGrams { get; private set; }

    public DateTime AssignedAt { get; private set; } = DateTime.UtcNow;

    //Yeni bir baskı işi oluşturur.
    public PrintJob(string name, double estimatedDurationHours, double filamentRequiredGrams)
    {
        Name = name;
        EstimatedDurationHours = estimatedDurationHours;
        FilamentRequiredGrams = filamentRequiredGrams;
    }
}
