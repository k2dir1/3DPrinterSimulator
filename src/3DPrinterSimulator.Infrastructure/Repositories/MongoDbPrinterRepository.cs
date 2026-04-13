using _3DPrinterSimulator.Data.Entities;
using _3DPrinterSimulator.Data.Interfaces;
using MongoDB.Driver;

namespace _3DPrinterSimulator.Infrastructure.Repositories;

public class MongoDbPrinterRepository : IPrinterRepository
{
    private readonly IMongoCollection<Printer> _collection;

    public MongoDbPrinterRepository(IMongoClient client, MongoDbSettings settings)
    {
        var db = client.GetDatabase(settings.DatabaseName);
        _collection = db.GetCollection<Printer>("printers");
    }

    public async Task<IReadOnlyList<Printer>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _collection.Find(_ => true).ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task<Printer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _collection.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
        return result;
    }

    public async Task AddAsync(Printer printer, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(printer, cancellationToken: ct);
    }

    public async Task UpdateAsync(Printer printer, CancellationToken ct = default)
    {
        await _collection.ReplaceOneAsync(p => p.Id == printer.Id, printer, cancellationToken: ct);
    }
}