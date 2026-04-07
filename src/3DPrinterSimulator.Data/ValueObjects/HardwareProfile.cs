namespace _3DPrinterSimulator.Data.ValueObjects;

/// <summary>Yazıcının değişmeyen donanım limitleri.</summary>
/// <param name="MaxSpeedMmPerSec">Maksimum baskı hızı (mm/s).</param>
/// <param name="MaxTempCelsius">Güvenli maksimum sıcaklık (°C).</param>
/// <param name="AccelerationMmPerSec2">İvme (mm/s²).</param>
/// <param name="FilamentCapacityGrams">Makara kapasitesi (gram).</param>
public record HardwareProfile(
    int MaxSpeedMmPerSec,
    int MaxTempCelsius,
    int AccelerationMmPerSec2,
    double FilamentCapacityGrams
);
