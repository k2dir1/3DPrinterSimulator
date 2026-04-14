using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace _3DPrinterSimulator.Infrastructure.Messaging;

public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConnection> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);


    public const string JobExchange = "printer.jobs";
    public const string DeadLetterExchange = "printer.jobs.dlx";
    public const string RetryExchange = "printer.jobs.retry";
    public const string SingleColorQueue = "jobs.singlecolor";
    public const string MultiColorQueue = "jobs.multicolor";
    public const string AnyPrinterQueue = "jobs.any";
    public const string DeadLetterQueue = "jobs.deadletter";
    public const string RetryQueue = "jobs.retry";

    public RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IChannel> GetChannelAsync()
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _lock.WaitAsync();
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await SetupTopologyAsync(_channel);

            _logger.LogInformation("RabbitMQ connection established and topology created");
            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SetupTopologyAsync(IChannel channel)
    {
        //deadletter
        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange,
            type: ExchangeType.Fanout,
            durable: true);

        await channel.QueueDeclareAsync(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, "");

        //retry
        await channel.ExchangeDeclareAsync(
            exchange: RetryExchange,
            type: ExchangeType.Fanout,
            durable: true);

        await channel.QueueDeclareAsync(
            queue: RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                { "x-message-ttl", _options.RetryDelayMs },
                { "x-dead-letter-exchange", JobExchange },
                { "x-dead-letter-routing-key", "job.any" }
            });

        await channel.QueueBindAsync(RetryQueue, RetryExchange, "");

        //exchange
        await channel.ExchangeDeclareAsync(
            exchange: JobExchange,
            type: ExchangeType.Topic,
            durable: true);

        //single
        await channel.QueueDeclareAsync(
            queue: SingleColorQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                { "x-max-priority", 10 },
                { "x-dead-letter-exchange", DeadLetterExchange }
            });


        await channel.QueueDeclareAsync( //multi
            queue: MultiColorQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                { "x-max-priority", 10 },
                { "x-dead-letter-exchange", DeadLetterExchange }
            });

        await channel.QueueDeclareAsync( //any
            queue: AnyPrinterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                { "x-max-priority", 10 },
                { "x-dead-letter-exchange", DeadLetterExchange }
            });

        //bindings 
        await channel.QueueBindAsync(SingleColorQueue, JobExchange, "job.singlecolor");

        await channel.QueueBindAsync(MultiColorQueue, JobExchange, "job.multicolor");


        await channel.QueueBindAsync(AnyPrinterQueue, JobExchange, "job.any");

   

        _logger.LogInformation(
            "RabbitMQ topology ready: exchanges [{Job}, {DLX}, {Retry}], queues [{SC}, {MC}, {Any}, {DL}, {R}]",
            JobExchange, DeadLetterExchange, RetryExchange,
            SingleColorQueue, MultiColorQueue, AnyPrinterQueue, DeadLetterQueue, RetryQueue);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
    }
}