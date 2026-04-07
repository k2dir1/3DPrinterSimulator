using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Infrastructure;
using _3DPrinterSimulator.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<IPrinterRepository>();
    await PrinterSeeder.SeedAsync(repo);
    app.Logger.LogInformation("10 yazýcý baþarýyla oluþturuldu.");
}

app.Run();
