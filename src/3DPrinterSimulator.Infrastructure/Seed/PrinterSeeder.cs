using _3DPrinterSimulator.Data.Entities;
using _3DPrinterSimulator.Data.Enums;
using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Data.StateMachine;
using _3DPrinterSimulator.Data.ValueObjects;
using Bogus;

namespace _3DPrinterSimulator.Infrastructure.Seed;

//Uygulama başlangıcında 10 farklı markalı yazıcı oluşturur.
public static class PrinterSeeder
{
    private static readonly (string Brand, string Model, bool IsMulti)[] Catalog =
    [
        ("Creality",   "Ender-3 Neo",    false),
        ("Creality",   "K1 Max",         false),
        ("Bambu Lab",  "P1S",            true),
        ("Bambu Lab",  "X1 Carbon",      true),
        ("Prusa",      "MK4",            false),
        ("Prusa",      "XL",             true),
        ("Anycubic",   "Kobra 2 Pro",    false),
        ("Elegoo",     "Neptune 4 Pro",  false),
        ("Flashforge", "Creator 4",      true),
        ("Raise3D",    "Pro3 Plus",      false),
    ];

    public static async Task SeedAsync(IPrinterRepository repo)
    {
        var faker = new Faker("tr");
        var rng = new Random(42); // tekrarlanabilir sonuçlar için sabit seed

        for (int i = 0; i < Catalog.Length; i++)
        {
            var (brand, model, isMulti) = Catalog[i];

            var profile = new HardwareProfile(
                MaxSpeedMmPerSec: rng.Next(150, 601),
                MaxTempCelsius: rng.Next(260, 361),
                AccelerationMmPerSec2: rng.Next(1_000, 10_001),
                FilamentCapacityGrams: rng.Next(800, 2_001)
            );

            var printer = new Printer(
                name: $"{brand} #{i + 1}",
                brand: brand,
                model: model,
                printerType: isMulti ? PrinterType.MultiColor : PrinterType.SingleColor,
                hardwareProfile: profile
            );

            var sm = new PrinterStateMachine(printer);
            sm.Fire(PrinterStateMachine.Triggers.PowerOn);

            await repo.AddAsync(printer);
        }
    }
}
