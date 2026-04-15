using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using _3DPrinterSimulator.Application.Contracts;
using _3DPrinterSimulator.Data.Interfaces;

namespace _3DPrinterSimulator.Infrastructure.Messaging;

public class RabbitMqProducer : IRabbitMqProducer
{
    private readonly RabbitMqConnection _connection;
    private readonly ILogger<RabbitMqProducer> _logger;

    public RabbitMqProducer(RabbitMqConnection connection, ILogger<RabbitMqProducer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task PublishJobAsync(PrintJobMessage message)
    {
        var channel = await _connection.GetChannelAsync();

        var routingKey = message.TargetPrinterType.ToLower() switch
        {
            "singlecolor" => "job.singlecolor",
            "multicolor" => "job.multicolor",
            _ => "job.any"
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var props = new BasicProperties
        {
            Persistent = true,
            Priority = message.Priority,
            MessageId = message.JobId.ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            ContentType = "application/json",
            Headers = new Dictionary<string, object?>
            {
                { "retry-count", message.RetryCount },
                { "created-at", message.CreatedAt.ToString("O") }
            }
        };

        await channel.BasicPublishAsync(
            exchange: RabbitMqConnection.JobExchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body);

        _logger.LogInformation(
            "Published job {Name} → {Exchange}/{Route} [priority={Pri}]",
            message.Name, RabbitMqConnection.JobExchange, routingKey, message.Priority);
    }

    public async Task PublishBatchAsync(IEnumerable<PrintJobMessage> messages)
    {
        var channel = await _connection.GetChannelAsync();

        foreach (var message in messages)
        {
            await PublishJobAsync(message);
        }

        _logger.LogInformation("Batch publish complete");
    }
}