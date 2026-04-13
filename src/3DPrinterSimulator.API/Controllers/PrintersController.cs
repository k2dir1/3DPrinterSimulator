using _3DPrinterSimulator.Application.Commands;
using _3DPrinterSimulator.Application.DTOs;
using _3DPrinterSimulator.Data.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost("{id:guid}/trigger")]
    public async Task<IActionResult> Trigger(Guid id, [FromBody] TriggerDto dto)
    {
        if (dto.Trigger == "JobAssigned")
        {
            var result = await _mediator.Send(
                new AssignJobCommand(id, dto.Name ?? "ManualJob",
                    dto.EstimatedDurationHours ?? 2.0,
                    dto.FilamentRequiredGrams ?? 50.0));
            return Ok(result);
        }
        if (dto.Trigger == "Resume")
            return Ok(await _mediator.Send(new ResumeCommand(id)));
        if (dto.Trigger == "Reset")
            return Ok(await _mediator.Send(new ResetCommand(id)));
        return BadRequest("Unknown trigger");
    }


    [HttpPost("blast")]
    public async Task<IActionResult> BlastJobs([FromQuery] int count = 100)
    {

        var resultMessage = await _mediator.Send(new GenerateBulkJobsCommand(count));

        return Ok(new
        {
            Message = resultMessage,
            Timestamp = DateTime.UtcNow
        });
    }
}