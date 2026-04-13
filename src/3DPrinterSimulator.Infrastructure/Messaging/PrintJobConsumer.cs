using MassTransit;
using Microsoft.Extensions.Logging;
using _3DPrinterSimulator.Application.Contracts;
using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Data.StateMachine;
using _3DPrinterSimulator.Data.Entities;

namespace _3DPrinterSimulator.Infrastructure.Messaging;

public class PrintJobConsumer : IConsumer<PrintJobRequest>
{
    private readonly IPrinterRepository _repo;
    private readonly ILogger<PrintJobConsumer> _logger;

    public PrintJobConsumer(IPrinterRepository repo, ILogger<PrintJobConsumer> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PrintJobRequest> context)
    {
        var jobData = context.Message;
        _logger.LogInformation("New job from RabbitMQ: {Name}", jobData.Name);

        var printers = await _repo.GetAllAsync();
        var idlePrinter = printers.FirstOrDefault(p => p.Status == _3DPrinterSimulator.Data.Enums.PrinterStatus.Idle);

        if (idlePrinter == null)
        {
            _logger.LogWarning("All printers busy. Retrying soon...");
            throw new Exception("No idle printers.");
        }

        var job = new PrintJob(jobData.Name, jobData.Duration, jobData.FilamentGrams);
        idlePrinter.AssignJob(job);

        var sm = new PrinterStateMachine(idlePrinter);
        sm.Fire(PrinterStateMachine.Triggers.JobAssigned);

        await _repo.UpdateAsync(idlePrinter);
        _logger.LogInformation("Assigned {Job} to {Printer}", job.Name, idlePrinter.Name);
    }
}