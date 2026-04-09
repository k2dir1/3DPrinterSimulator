
using AutoMapper;
using MediatR;
using _3DPrinterSimulator.Application.Commands;
using _3DPrinterSimulator.Application.DTOs;
using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Data.StateMachine;

namespace _3DPrinterSimulator.Application.Handlers;

public class ResetHandler : IRequestHandler<ResetCommand, PrinterDto>
{
    private readonly IPrinterRepository _repo;
    private readonly IMapper _mapper;

    public ResetHandler(IPrinterRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<PrinterDto> Handle(ResetCommand request, CancellationToken ct)
    {
        var printer = await _repo.GetByIdAsync(request.PrinterId, ct)
            ?? throw new KeyNotFoundException($"Printer {request.PrinterId} not found");

        var sm = new PrinterStateMachine(printer);

        if (!sm.CanFire(PrinterStateMachine.Triggers.Reset))
            throw new InvalidOperationException(
                $"Printer is {printer.Status}, cannot reset. Permitted: {string.Join(", ", sm.PermittedTriggers)}");

        printer.Reset();
        sm.Fire(PrinterStateMachine.Triggers.Reset);
        await _repo.UpdateAsync(printer, ct);
        return _mapper.Map<PrinterDto>(printer);
    }
}