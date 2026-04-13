using MediatR;

namespace _3DPrinterSimulator.Application.Commands;

public record GenerateBulkJobsCommand(int Count) : IRequest<string>;