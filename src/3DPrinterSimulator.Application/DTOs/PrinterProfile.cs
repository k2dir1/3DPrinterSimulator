using AutoMapper;
using _3DPrinterSimulator.Application.DTOs;
using _3DPrinterSimulator.Data.Entities;
using _3DPrinterSimulator.Data.StateMachine;

namespace _3DPrinterSimulator.Application.Mapping;

public class PrinterProfile : Profile
{
    public PrinterProfile()
    {
        CreateMap<Printer, PrinterDto>()
            .ForMember(d => d.PrinterType, o => o.MapFrom(s => s.PrinterType.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CurrentJobName, o => o.MapFrom(s => s.CurrentJob != null ? s.CurrentJob.Name : null))
            .ForMember(d => d.PermittedTriggers, o => o.MapFrom(s => new PrinterStateMachine(s).PermittedTriggers));
    }
}