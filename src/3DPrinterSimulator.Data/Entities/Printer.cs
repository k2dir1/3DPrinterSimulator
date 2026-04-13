using _3DPrinterSimulator.Data.Enums;
using _3DPrinterSimulator.Data.Events;
using _3DPrinterSimulator.Data.ValueObjects;

namespace _3DPrinterSimulator.Data.Entities;

public class Printer
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; }

    public string Brand { get; private set; }

    public string Model { get; private set; }

    public PrinterType PrinterType { get; private set; }

    public HardwareProfile HardwareProfile { get; private set; }

    //Geçerli yazıcı durumu; yalnızca state machine tarafından değiştirilir.
    public PrinterStatus Status { get; private set; } = PrinterStatus.Offline;

    public double NozzleTemp { get; private set; }

    public double TargetNozzleTemp { get; private set; }

    public double BedTemp { get; private set; }

    public double TargetBedTemp { get; private set; }

    public int Progress { get; private set; }

    public double FeedRate { get; private set; } = 1.0;

    public double FilamentRemainingGrams { get; private set; }

    public bool HasFilamentRunout { get; private set; }

    /// <summary>
    /// Filament config eşiğinin altında — uyarı seviyesi.
    /// Simülatör tarafından her tick'te güncellenir; persist edilmez.
    /// </summary>
    public bool HasFilamentWarning { get; private set; }

    public string? ErrorMessage { get; private set; }

    public PrintJob? CurrentJob { get; private set; }

    public DateTime? JobStartedAt { get; private set; }

    // ── Domain event'leri ─────────────────────────────────────────────────
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    //Yeni bir yazıcı oluşturur; başlangıç durumu Offline'dır.
    public Printer(string name, string brand, string model,
                   PrinterType printerType, HardwareProfile hardwareProfile)
    {
        Name = name;
        Brand = brand;
        Model = model;
        PrinterType = printerType;
        HardwareProfile = hardwareProfile;
        FilamentRemainingGrams = hardwareProfile.FilamentCapacityGrams;
    }

    // ── State Machine tarafından çağrılan tek durum değiştirici ──────────
    /// <summary>
    /// Yazıcı durumunu günceller.
    /// Yalnızca <see cref="PrinterFarm.Domain.StateMachine.PrinterStateMachine"/> tarafından çağrılmalıdır.
    /// </summary>
    public void SetStatus(PrinterStatus status) => Status = status;

    // ── İş operasyonları ──────────────────────────────────────────────────

    //Yazıcıya bir baskı işi atar ve ısınma parametrelerini ayarlar.
    public void AssignJob(PrintJob job)
    {
        CurrentJob = job;
        JobStartedAt = DateTime.UtcNow;
        TargetNozzleTemp = 210;
        TargetBedTemp = 60;
        Progress = 0;
        _domainEvents.Add(new JobAssignedEvent(Id, job.Id, DateTime.UtcNow));
    }

    // Filament makara değişimi simüle eder: kapasiteye doldurur, runout/warning bayraklarını temizler.
    public void RefillFilament()
    {
        FilamentRemainingGrams = HardwareProfile.FilamentCapacityGrams;
        HasFilamentRunout = false;
        HasFilamentWarning = false;
    }

    //İş tamamlandığında çağrılır; domain event ekler.
    public void CompleteJob()
    {
        if (CurrentJob is not null)
            _domainEvents.Add(new PrintCompletedEvent(Id, CurrentJob.Id, DateTime.UtcNow));
        Progress = 100;
    }

    //Simülatörden gelen anlık sensör verilerini günceller.
    public void UpdateSensorData(double nozzleTemp, double bedTemp, int progress, double feedRate)
    {
        NozzleTemp = nozzleTemp;
        BedTemp = bedTemp;
        Progress = Math.Clamp(progress, 0, 100);
        FeedRate = feedRate;
    }

    //Belirtilen miktarda filament tüketir.
    public void ConsumeFilament(double grams)
    {
        FilamentRemainingGrams = Math.Max(0, FilamentRemainingGrams - grams);

        if (FilamentRemainingGrams <= 0 && !HasFilamentRunout)
        {
            HasFilamentRunout = true;
            _domainEvents.Add(new FilamentRunoutEvent(Id, DateTime.UtcNow));
        }
    }

    // Filament uyarı durumunu config eşiğine göre günceller.
    public void UpdateFilamentWarning(double warningThresholdGrams)
    {
        HasFilamentWarning = FilamentRemainingGrams <= warningThresholdGrams && !HasFilamentRunout;
    }

    //Kritik sıcaklık tespitinde hata mesajı ayarlar ve event ekler.
    public void RaiseCriticalTemp(double temp)
    {
        ErrorMessage = $"Kritik sıcaklık tespit edildi: {temp:F1}°C";
        _domainEvents.Add(new CriticalTempEvent(Id, temp, DateTime.UtcNow));
    }

    //Hata durumunu ve iş bilgisini temizler; yazıcıyı sıfırlar.
    public void Reset()
    {
        ErrorMessage = null;
        HasFilamentRunout = false;
        HasFilamentWarning = false;
        CurrentJob = null;
        JobStartedAt = null;
        Progress = 0;
        NozzleTemp = 0;
        BedTemp = 0;
        TargetNozzleTemp = 0;
        TargetBedTemp = 0;
    }

    // ── Hesaplanan özellikler ─────────────────────────────────────────────
    /// <summary>
    /// Aktif işin tahmini kalan süresi (saat).
    /// Yalnızca Printing durumunda ve iş varsa hesaplanır; aksi hâlde null.
    /// </summary>
    public double? EstimatedRemainingHours =>
        CurrentJob is null || Status != Enums.PrinterStatus.Printing || Progress >= 100
            ? null
            : CurrentJob.EstimatedDurationHours * (1.0 - Progress / 100.0) / FeedRate;
}
