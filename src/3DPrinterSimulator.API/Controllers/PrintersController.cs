using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3DPrinterSimulator.Application.Commands;
using _3DPrinterSimulator.Application.DTOs;
using _3DPrinterSimulator.Data.Interfaces;

namespace _3DPrinterSimulator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPrinterRepository _repo;
    private readonly IMapper _mapper;

    public PrintersController(IMediator mediator, IPrinterRepository repo, IMapper mapper)
    {
        _mediator = mediator;
        _repo = repo;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrinterDto>>> GetAll(CancellationToken ct)
    {
        var printers = await _repo.GetAllAsync(ct);
        return Ok(_mapper.Map<IEnumerable<PrinterDto>>(printers));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PrinterDto>> GetById(Guid id, CancellationToken ct)
    {
        var printer = await _repo.GetByIdAsync(id, ct);
        if (printer is null) return NotFound();
        return Ok(_mapper.Map<PrinterDto>(printer));
    }

    [HttpPost("{id:guid}/assign-job")]
    public async Task<ActionResult<PrinterDto>> AssignJob(Guid id, [FromBody] AssignJobDto dto)
    {
        try
        {
            var result = await _mediator.Send(new AssignJobCommand(
                id, dto.Name, dto.EstimatedDurationHours, dto.FilamentRequiredGrams));
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<ActionResult<PrinterDto>> Resume(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new ResumeCommand(id));
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("{id:guid}/reset")]
    public async Task<ActionResult<PrinterDto>> Reset(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new ResetCommand(id));
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
}