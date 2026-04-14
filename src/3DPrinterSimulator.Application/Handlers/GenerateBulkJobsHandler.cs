using MediatR;
using Microsoft.Extensions.Logging;
using _3DPrinterSimulator.Application.Commands;
using _3DPrinterSimulator.Application.Contracts;
using _3DPrinterSimulator.Data.Interfaces;

namespace _3DPrinterSimulator.Application.Handlers;

public class GenerateBulkJobsHandler : IRequestHandler<GenerateBulkJobsCommand, string>
{
    private readonly IRabbitMqProducer _producer;
    private readonly ILogger<GenerateBulkJobsHandler> _logger;

    public GenerateBulkJobsHandler(IRabbitMqProducer producer, ILogger<GenerateBulkJobsHandler> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task<string> Handle(GenerateBulkJobsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating {Count} jobs for RabbitMQ...", request.Count);

        var random = new Random();
        var types = new[] { "singlecolor", "multicolor", "any" };
        var messages = new List<PrintJobMessage>();

        for (int i = 0; i < request.Count; i++)
        {
            messages.Add(new PrintJobMessage
            {
                JobId = Guid.NewGuid(),
                Name = $"Job-{i + 1}",
                Duration = Math.Round(random.NextDouble() * 9 + 1, 1),
                FilamentGrams = random.Next(50, 300),
                TargetPrinterType = types[random.Next(types.Length)],
                Priority = (byte)random.Next(0, 10)
            });
        }

        await _producer.PublishBatchAsync(messages);

        _logger.LogInformation("Dispatched {Count} jobs to RabbitMQ", request.Count);
        return $"Blasted {request.Count} jobs into RabbitMQ with random priorities and printer types!";
    }
}