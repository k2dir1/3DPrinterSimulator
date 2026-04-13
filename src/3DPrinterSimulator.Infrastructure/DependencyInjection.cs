using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Infrastructure.Repositories;
using _3DPrinterSimulator.Infrastructure.Simulation;
using _3DPrinterSimulator.Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Security.Authentication;

namespace _3DPrinterSimulator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MongoDB global Guid ayarý — en baþta yapýlmalý
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        // private set olan property'leri MongoDB okuyabilsin
        ConventionRegistry.Register("IgnoreExtraElements",
            new ConventionPack { new IgnoreExtraElementsConvention(true) }, _ => true);

        services.Configure<SimulationOptions>(
            configuration.GetSection(SimulationOptions.SectionName));

        var mongoSettings = new MongoDbSettings
        {
            ConnectionString = configuration["MONGODB_CONNECTION_STRING"]
                ?? configuration["MongoDB:ConnectionString"]
                ?? "mongodb://localhost:27017",
            DatabaseName = configuration["MongoDB:DatabaseName"] ?? "3dprinter"
        };

        var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoSettings.ConnectionString);
        mongoClientSettings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls12
        };

        services.AddSingleton<IMongoClient>(new MongoClient(mongoClientSettings));
        services.AddSingleton(mongoSettings);
        services.AddSingleton<IPrinterRepository, MongoDbPrinterRepository>();

        services.AddHostedService<PrinterSimulatorService>();

        // --- Advanced RabbitMQ Setup ---
        services.AddMassTransit(x =>
        {

            x.AddConsumer<PrintJobConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {

                cfg.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });


                cfg.PrefetchCount = 10;

                cfg.ConcurrentMessageLimit = 1;


                cfg.UseMessageRetry(r =>
                {
                    r.Interval(1000, TimeSpan.FromSeconds(5));
                });

                // 3. Circuit Breaker: Pause the queue if the system crashes too much
                //cfg.UseCircuitBreaker(cb =>
                //{
                //    cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                //    cb.TripThreshold = 15; // Stop if 15% of jobs fail
                //    cb.ActiveThreshold = 10;
                //    cb.ResetInterval = TimeSpan.FromMinutes(5); // Wait 5 mins before testing again
                //});


                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}