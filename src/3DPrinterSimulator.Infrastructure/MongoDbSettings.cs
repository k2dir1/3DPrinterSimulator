namespace _3DPrinterSimulator.Infrastructure;

public class MongoDbSettings
{
    public const string SectionName = "MongoDB";
    public string ConnectionString { get; init; } = default!;
    public string DatabaseName { get; init; } = default!;
}