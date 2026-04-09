using _3DPrinterSimulator.Data.Entities;
using _3DPrinterSimulator.Data.Enums;
using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Data.StateMachine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace _3DPrinterSimulator.Infrastructure.Simulation;

public sealed class PrinterSimulatorService : BackgroundService
{
    private readonly IPrinterRepository _repo;
    private readonly ILogger<PrinterSimulatorService> _logger;
    private readonly SimulationOptions _options;
    private readonly IPrinterBroadcaster _broadcaster;
    private readonly Random _rng = new();

    public PrinterSimulatorService(
        IPrinterRepository repo,
        ILogger<PrinterSimulatorService> logger,
        IOptions<SimulationOptions> options,
        IPrinterBroadcaster broadcaster)
    {
        _repo = repo;
        _logger = logger;
        _options = options.Value;
        _broadcaster = broadcaster;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "PrinterSimulator başlatıldı. Tick: {Interval}ms, AutoJob: {Prob:P0}, FilamentEşiği: {Threshold}g",
            _options.TickIntervalMs,
            _options.AutoJobProbability,
            _options.FilamentWarningThresholdGrams);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_options.TickIntervalMs, stoppingToken);

            var printers = await _repo.GetAllAsync(stoppingToken);
            await Task.WhenAll(
                printers.Select(p => TickPrinterAsync(p, stoppingToken))
            );
            await _broadcaster.BroadcastAllAsync(stoppingToken);
        }
    }

    private async Task TickPrinterAsync(Printer printer, CancellationToken ct)
    {
        var sm = new PrinterStateMachine(printer);

        switch (printer.Status)
        {
            case PrinterStatus.Idle:
                await TickIdleAsync(printer, sm);
                break;

            case PrinterStatus.Heating:
                TickHeating(printer, sm);
                break;

            case PrinterStatus.Printing:
                TickPrinting(printer, sm);
                break;

            case PrinterStatus.Cooling:
                TickCooling(printer, sm);
                break;

            case PrinterStatus.Paused:
                // Sıcaklık korunur; kullanıcı Resume komutu gönderene kadar beklenir
                break;

            case PrinterStatus.Error:
                // Kullanıcı Reset komutu gönderene kadar beklenir
                break;
        }

        // Filament uyarı bayrağını her tick'te güncelle
        printer.UpdateFilamentWarning(_options.FilamentWarningThresholdGrams);

        await _repo.UpdateAsync(printer, ct);
    }


    private async Task TickIdleAsync(Printer printer, PrinterStateMachine sm)
    {
        if (_rng.NextDouble() >= _options.AutoJobProbability) return;

        var job = new PrintJob(
            name: $"AutoJob-{Guid.NewGuid().ToString()[..8]}",
            estimatedDurationHours: _rng.NextDouble() * 8 + 0.5,
            filamentRequiredGrams: _rng.NextDouble() * 150 + 10
        );

        printer.AssignJob(job);
        sm.Fire(PrinterStateMachine.Triggers.JobAssigned);
        _logger.LogInformation("{Name} -> Heating (otomatik iş atandı: {Job})", printer.Name, job.Name);

        await Task.CompletedTask;
    }

    private void TickHeating(Printer printer, PrinterStateMachine sm)
    {
        var newNozzle = printer.NozzleTemp + _rng.NextDouble() * 15 + 5;
        var newBed = printer.BedTemp + _rng.NextDouble() * 4 + 1;

        printer.UpdateSensorData(newNozzle, newBed, printer.Progress, printer.FeedRate);

        // Kritik sıcaklık kontrolü
        if (newNozzle > printer.HardwareProfile.MaxTempCelsius)
        {
            printer.RaiseCriticalTemp(newNozzle);
            sm.Fire(PrinterStateMachine.Triggers.CriticalTemp);
            _logger.LogWarning("{Name} -> Error (kritik sıcaklık: {Temp:F1}°C)", printer.Name, newNozzle);
            return;
        }

        // Hedef sıcaklığa ulaşıldı mı?
        if (newNozzle >= printer.TargetNozzleTemp)
        {
            sm.Fire(PrinterStateMachine.Triggers.TempReached);
            _logger.LogInformation("{Name} -> Printing (hedef sıcaklığa ulaşıldı)", printer.Name);
        }
    }

    private void TickPrinting(Printer printer, PrinterStateMachine sm)
    {
        // Progress artışı
        var progressInc = (int)(_rng.NextDouble() * 5 + 1);
        var newProgress = Math.Min(100, printer.Progress + progressInc);
        var feedRate = 0.9 + _rng.NextDouble() * 0.2;

        // Sıcaklık dalgalanması
        var newNozzle = printer.NozzleTemp + (_rng.NextDouble() - 0.5) * 3;
        var newBed = printer.BedTemp + (_rng.NextDouble() - 0.5) * 2;

        printer.UpdateSensorData(newNozzle, newBed, newProgress, feedRate);
        printer.ConsumeFilament(_rng.NextDouble() * 2 + 0.3);

        // Filament bitti mi?
        if (printer.HasFilamentRunout && sm.CanFire(PrinterStateMachine.Triggers.FilamentRunout))
        {
            sm.Fire(PrinterStateMachine.Triggers.FilamentRunout);
            _logger.LogWarning("{Name} -> Paused (filament bitti)", printer.Name);
            return;
        }

        // Kritik sıcaklık?
        if (newNozzle > printer.HardwareProfile.MaxTempCelsius)
        {
            printer.RaiseCriticalTemp(newNozzle);
            sm.Fire(PrinterStateMachine.Triggers.CriticalTemp);
            _logger.LogWarning("{Name} -> Error (baskı sırasında kritik sıcaklık)", printer.Name);
            return;
        }

        // Baskı tamamlandı mı?
        if (newProgress >= 100)
        {
            printer.CompleteJob();
            sm.Fire(PrinterStateMachine.Triggers.Progress100);
            _logger.LogInformation("{Name} -> Cooling (baskı tamamlandı)", printer.Name);
        }
    }

    private void TickCooling(Printer printer, PrinterStateMachine sm)
    {
        var newNozzle = Math.Max(22, printer.NozzleTemp - (_rng.NextDouble() * 12 + 5));
        var newBed = Math.Max(22, printer.BedTemp - (_rng.NextDouble() * 8 + 3));

        printer.UpdateSensorData(newNozzle, newBed, printer.Progress, printer.FeedRate);

        if (newNozzle < 40 && newBed < 40)
        {
            sm.Fire(PrinterStateMachine.Triggers.TempBelow40);
            _logger.LogInformation("{Name} → Idle (soğuma tamamlandı)", printer.Name);
        }
    }
}
