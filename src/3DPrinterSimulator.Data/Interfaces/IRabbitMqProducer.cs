using _3DPrinterSimulator.Application.Contracts;

namespace _3DPrinterSimulator.Data.Interfaces;

public interface IRabbitMqProducer
{
    Task PublishJobAsync(PrintJobMessage message);
    Task PublishBatchAsync(IEnumerable<PrintJobMessage> messages);
}