using MediatR;
using _3DPrinterSimulator.Application.DTOs;

namespace _3DPrinterSimulator.Application.Commands;

public record ResumeCommand(Guid PrinterId) : IRequest<PrinterDto>;