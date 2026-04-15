namespace _3DPrinterSimulator.Infrastructure.Messaging;

public record RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
    public int MaxRetryCount { get; init; } = 3;
    public int RetryDelayMs { get; init; } = 30_000;
}