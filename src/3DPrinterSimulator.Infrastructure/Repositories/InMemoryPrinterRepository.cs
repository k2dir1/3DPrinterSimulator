using _3DPrinterSimulator.Data.Entities;
using _3DPrinterSimulator.Data.Interfaces;
using System.Collections.Concurrent;

namespace _3DPrinterSimulator.Infrastructure.Repositories;

public class InMemoryPrinterRepository : IPrinterRepository
{
    private readonly ConcurrentDictionary<Guid, Printer> _store = new();

    public Task<IReadOnlyList<Printer>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Printer>>(_store.Values.ToList());

    public Task<Printer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var printer) ? printer : null);

    public Task AddAsync(Printer printer, CancellationToken ct = default)
    {
        _store[printer.Id] = printer;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Printer printer, CancellationToken ct = default)
    {
        _store[printer.Id] = printer;
        return Task.CompletedTask;
    }
}
