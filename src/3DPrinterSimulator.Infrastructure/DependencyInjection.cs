using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Infrastructure.Repositories;
using _3DPrinterSimulator.Infrastructure.Simulation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace _3DPrinterSimulator.Infrastructure;


public static class DependencyInjection
{

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SimulationOptions>(
            configuration.GetSection(SimulationOptions.SectionName));

        services.AddSingleton<IPrinterRepository, InMemoryPrinterRepository>();

        services.AddHostedService<PrinterSimulatorService>();

        return services;
    }
}
