using AutoMapper;
using MediatR;
using _3DPrinterSimulator.Application.Commands;
using _3DPrinterSimulator.Application.DTOs;
using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Data.StateMachine;

namespace _3DPrinterSimulator.Application.Handlers;

public class ResumeHandler : IRequestHandler<ResumeCommand, PrinterDto>
{
    private readonly IPrinterRepository _repo;
    private readonly IMapper _mapper;

    public ResumeHandler(IPrinterRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<PrinterDto> Handle(ResumeCommand request, CancellationToken ct)
    {
        var printer = await _repo.GetByIdAsync(request.PrinterId, ct)
            ?? throw new KeyNotFoundException($"Printer {request.PrinterId} not found");

        var sm = new PrinterStateMachine(printer);

        if (!sm.CanFire(PrinterStateMachine.Triggers.Resume))
            throw new InvalidOperationException(
                $"Printer is {printer.Status}, cannot resume. Permitted: {string.Join(", ", sm.PermittedTriggers)}");

        sm.Fire(PrinterStateMachine.Triggers.Resume);
        await _repo.UpdateAsync(printer, ct);
        return _mapper.Map<PrinterDto>(printer);
    }
}