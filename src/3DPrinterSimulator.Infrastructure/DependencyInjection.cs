using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Infrastructure.Repositories;
using _3DPrinterSimulator.Infrastructure.Simulation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace _3DPrinterSimulator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SimulationOptions>(
            configuration.GetSection(SimulationOptions.SectionName));

        var mongoSettings = new MongoDbSettings
        {
            ConnectionString = configuration["MONGODB_CONNECTION_STRING"]
                ?? configuration["MongoDB:ConnectionString"]
                ?? "mongodb://localhost:27017",
            DatabaseName = configuration["MongoDB:DatabaseName"] ?? "3dprinter"
        };

        services.AddSingleton<IMongoClient>(
            new MongoClient(mongoSettings.ConnectionString));
        services.AddSingleton(mongoSettings);
        services.AddSingleton<IPrinterRepository, MongoDbPrinterRepository>();

        services.AddHostedService<PrinterSimulatorService>();

        return services;
    }
}