using MediatR;
using MassTransit;
using Microsoft.Extensions.Logging;
using _3DPrinterSimulator.Application.Commands;
using _3DPrinterSimulator.Application.Contracts;

namespace _3DPrinterSimulator.Application.Handlers;

public class GenerateBulkJobsHandler : IRequestHandler<GenerateBulkJobsCommand, string>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<GenerateBulkJobsHandler> _logger;

    public GenerateBulkJobsHandler(IPublishEndpoint publishEndpoint, ILogger<GenerateBulkJobsHandler> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<string> Handle(GenerateBulkJobsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Preparing to send {Count} jobs to RabbitMQ...", request.Count);

        var tasks = new List<Task>();
        var random = new Random();

        for (int i = 0; i < request.Count; i++)
        {
            var job = new PrintJobRequest
            {
                JobId = Guid.NewGuid(),
                Name = $"Bulk-Job-{i + 1}",
                Duration = Math.Round(random.NextDouble() * 9 + 1, 1),
                FilamentGrams = random.Next(50, 300)
            };


            tasks.Add(_publishEndpoint.Publish(job, cancellationToken));
        }


        await Task.WhenAll(tasks);

        _logger.LogInformation("Successfully dispatched {Count} jobs to the queue.", request.Count);

        return $"Successfully blasted {request.Count} jobs into the RabbitMQ exchange!";
    }
}