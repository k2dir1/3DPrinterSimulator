using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using _3DPrinterSimulator.Application.Contracts;
using _3DPrinterSimulator.Data.Entities;
using _3DPrinterSimulator.Data.Enums;
using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Data.StateMachine;

namespace _3DPrinterSimulator.Infrastructure.Messaging;

public class RabbitMqConsumer : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly IPrinterRepository _repo;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly RabbitMqOptions _options;

    public RabbitMqConsumer(
        RabbitMqConnection connection,
        IPrinterRepository repo,
        ILogger<RabbitMqConsumer> logger,
        IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _repo = repo;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await _connection.GetChannelAsync();


        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);


        var queues = new[] //listen icin
        {
            RabbitMqConnection.SingleColorQueue,
            RabbitMqConnection.MultiColorQueue,
            RabbitMqConnection.AnyPrinterQueue
        };

        foreach (var queue in queues)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                await HandleMessageAsync(channel, ea, queue);
            };

            await channel.BasicConsumeAsync(
                queue: queue,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Consuming from queue: {Queue}", queue);
        }


        await Task.Delay(Timeout.Infinite, stoppingToken); //kapatma
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea, string sourceQueue)
    {
        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<PrintJobMessage>(body);

            if (message is null)
            {
                _logger.LogWarning("Received null message, rejecting");
                await channel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
                return;
            }


            var printers = await _repo.GetAllAsync();
            var idlePrinter = FindMatchingPrinter(printers, sourceQueue); //find  idle

            if (idlePrinter is null)
            {

                _logger.LogWarning("No idle printer for queue {Queue}, requeueing {Job}", sourceQueue, message.Name);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true); //no idle
                return;
            }


            var job = new PrintJob(message.Name, message.Duration, message.FilamentGrams); //assign to idle
            idlePrinter.AssignJob(job);

            var sm = new PrinterStateMachine(idlePrinter);
            sm.Fire(PrinterStateMachine.Triggers.JobAssigned);

            await _repo.UpdateAsync(idlePrinter);


            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false); //assigned

            _logger.LogInformation(
                "✅ {Job} assigned to {Printer} (from {Queue}) [priority={Pri}, retry={Retry}]",
                message.Name, idlePrinter.Name, sourceQueue, message.Priority, message.RetryCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {Queue}", sourceQueue);
            await HandleFailureAsync(channel, ea, sourceQueue);
        }
    }

    private Printer? FindMatchingPrinter(IReadOnlyList<Printer> printers, string sourceQueue)
    {
        return sourceQueue switch
        {
            RabbitMqConnection.SingleColorQueue =>
                printers.FirstOrDefault(p => p.Status == PrinterStatus.Idle && p.PrinterType == PrinterType.SingleColor),

            RabbitMqConnection.MultiColorQueue =>
                printers.FirstOrDefault(p => p.Status == PrinterStatus.Idle && p.PrinterType == PrinterType.MultiColor),

            RabbitMqConnection.AnyPrinterQueue =>
                printers.FirstOrDefault(p => p.Status == PrinterStatus.Idle),

            _ => printers.FirstOrDefault(p => p.Status == PrinterStatus.Idle)
        };
    }

    private async Task HandleFailureAsync(IChannel channel, BasicDeliverEventArgs ea, string sourceQueue)
    {
        //retry sayısı header'da tutuluyor, yoksa 0 kabul edilir
        var retryCount = 0;
        if (ea.BasicProperties.Headers?.TryGetValue("retry-count", out var retryObj) == true)
        {
            retryCount = Convert.ToInt32(retryObj);
        }

        if (retryCount >= _options.MaxRetryCount) //max exceeded
        {

            _logger.LogWarning("Job exceeded max retries ({Max}), sending to dead letter queue", _options.MaxRetryCount);
            await channel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
        }
        else
        {
            //increase count
            _logger.LogWarning("Job failed, sending to retry queue (attempt {Retry}/{Max})", retryCount + 1, _options.MaxRetryCount);

            var body = ea.Body.ToArray();
            var message = JsonSerializer.Deserialize<PrintJobMessage>(Encoding.UTF8.GetString(body));

            if (message is not null)
            {
                var retryMessage = message with { RetryCount = retryCount + 1 };
                var retryBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(retryMessage));

                var props = new BasicProperties
                {
                    Persistent = true,
                    Priority = ea.BasicProperties.Priority,
                    MessageId = ea.BasicProperties.MessageId,
                    ContentType = "application/json",
                    Headers = new Dictionary<string, object?>
                    {
                        { "retry-count", retryCount + 1 },
                        { "original-queue", sourceQueue }
                    }
                };

                await channel.BasicPublishAsync(
                    exchange: RabbitMqConnection.RetryExchange,
                    routingKey: "",
                    mandatory: false,
                    basicProperties: props,
                    body: retryBody);
            }


            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false); //ack to remove from original queue
        }
    }
}