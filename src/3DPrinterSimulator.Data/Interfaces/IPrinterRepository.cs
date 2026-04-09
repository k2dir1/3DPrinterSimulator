using _3DPrinterSimulator.Data.Entities;
namespace _3DPrinterSimulator.Data.Interfaces;

public interface IPrinterRepository
{
    //Tüm yazıcıları döner.
    Task<IReadOnlyList<Printer>> GetAllAsync(CancellationToken ct = default);

    //Kimliğe göre yazıcı döner; bulunamazsa null.
    Task<Printer?> GetByIdAsync(Guid id, CancellationToken ct = default);

    //Yeni yazıcı ekler.
    Task AddAsync(Printer printer, CancellationToken ct = default);

    //Mevcut yazıcıyı günceller.
    Task UpdateAsync(Printer printer, CancellationToken ct = default);
}
