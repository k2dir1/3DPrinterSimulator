using MediatR;
using _3DPrinterSimulator.Application.DTOs;

namespace _3DPrinterSimulator.Application.Commands;

public record AssignJobCommand(
    Guid PrinterId,
    string Name,
    double EstimatedDurationHours,
    double FilamentRequiredGrams
) : IRequest<PrinterDto>;