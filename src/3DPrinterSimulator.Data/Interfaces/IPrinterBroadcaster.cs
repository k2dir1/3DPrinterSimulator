namespace _3DPrinterSimulator.Data.Interfaces;

public interface IPrinterBroadcaster
{
    Task BroadcastAllAsync(CancellationToken ct = default);
}