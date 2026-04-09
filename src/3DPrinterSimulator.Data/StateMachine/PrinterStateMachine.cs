using _3DPrinterSimulator.Data.Entities;
using _3DPrinterSimulator.Data.Enums;
using Stateless;
namespace _3DPrinterSimulator.Data.StateMachine;

public class PrinterStateMachine
{
    public static class Triggers
    {
        public const string PowerOn = nameof(PowerOn);
        public const string JobAssigned = nameof(JobAssigned);
        public const string TempReached = nameof(TempReached);
        public const string Progress100 = nameof(Progress100);
        public const string FilamentRunout = nameof(FilamentRunout);
        public const string CriticalTemp = nameof(CriticalTemp);
        public const string Resume = nameof(Resume);
        public const string TempBelow40 = nameof(TempBelow40);
        public const string Reset = nameof(Reset);
    }

    private readonly StateMachine<PrinterStatus, string> _machine;
    private readonly Printer _printer;

    //Verilen yazıcı için state machine oluşturur.
    public PrinterStateMachine(Printer printer)
    {
        _printer = printer;
        _machine = new StateMachine<PrinterStatus, string>(
            stateAccessor: () => _printer.Status,
            stateMutator: s => _printer.SetStatus(s)
        );
        Configure();
    }

    private void Configure()
    {
        _machine.Configure(PrinterStatus.Offline)
            .Permit(Triggers.PowerOn, PrinterStatus.Idle);

        _machine.Configure(PrinterStatus.Idle)
            .Permit(Triggers.JobAssigned, PrinterStatus.Heating);

        _machine.Configure(PrinterStatus.Heating)
            .Permit(Triggers.TempReached, PrinterStatus.Printing)
            .Permit(Triggers.CriticalTemp, PrinterStatus.Error);

        _machine.Configure(PrinterStatus.Printing)
            .Permit(Triggers.Progress100, PrinterStatus.Cooling)
            .Permit(Triggers.FilamentRunout, PrinterStatus.Paused)
            .Permit(Triggers.CriticalTemp, PrinterStatus.Error);

        _machine.Configure(PrinterStatus.Cooling)
            .Permit(Triggers.TempBelow40, PrinterStatus.Idle);

        _machine.Configure(PrinterStatus.Paused)
            .Permit(Triggers.Resume, PrinterStatus.Printing)
            .Permit(Triggers.CriticalTemp, PrinterStatus.Error);

        _machine.Configure(PrinterStatus.Error)
            .Permit(Triggers.Reset, PrinterStatus.Idle);
    }

    //Belirtilen trigger'ın şu an tetiklenip tetiklenemeyeceğini döner.
    public bool CanFire(string trigger) => _machine.CanFire(trigger);

    //Trigger'ı ateşler ve durum geçişini gerçekleştirir.
    public void Fire(string trigger) => _machine.Fire(trigger);

    //Şu an geçerli olan trigger'ların listesi.
    public IEnumerable<string> PermittedTriggers => _machine.PermittedTriggers;
}
