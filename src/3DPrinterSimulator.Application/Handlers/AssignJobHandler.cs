using AutoMapper;
using MediatR;
using _3DPrinterSimulator.Application.Commands;
using _3DPrinterSimulator.Application.DTOs;
using _3DPrinterSimulator.Data.Entities;
using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Data.StateMachine;

namespace _3DPrinterSimulator.Application.Handlers;

public class AssignJobHandler : IRequestHandler<AssignJobCommand, PrinterDto>
{
    private readonly IPrinterRepository _repo;
    private readonly IMapper _mapper;

    public AssignJobHandler(IPrinterRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<PrinterDto> Handle(AssignJobCommand request, CancellationToken ct)
    {
        var printer = await _repo.GetByIdAsync(request.PrinterId, ct)
            ?? throw new KeyNotFoundException($"Printer {request.PrinterId} not found");

        var sm = new PrinterStateMachine(printer);

        if (!sm.CanFire(PrinterStateMachine.Triggers.JobAssigned))
            throw new InvalidOperationException(
                $"Printer is {printer.Status}, cannot assign job. Permitted: {string.Join(", ", sm.PermittedTriggers)}");

        var job = new PrintJob(request.Name, request.EstimatedDurationHours, request.FilamentRequiredGrams);
        printer.AssignJob(job);
        sm.Fire(PrinterStateMachine.Triggers.JobAssigned);

        await _repo.UpdateAsync(printer, ct);
        return _mapper.Map<PrinterDto>(printer);
    }
}