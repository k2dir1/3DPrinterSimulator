namespace _3DPrinterSimulator.Data.Enums;

//Bir yazıcının anlık yaşam döngüsü durumu.
public enum PrinterStatus
{
    Offline,
    Idle,
    Heating,
    Printing,
    Cooling,
    Paused,
    Error
}
