// API/Services/SignalRPrinterBroadcaster.cs
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using _3DPrinterSimulator.API.Hubs;
using _3DPrinterSimulator.Application.DTOs;
using _3DPrinterSimulator.Data.Interfaces;

namespace _3DPrinterSimulator.API.Services;

public class SignalRPrinterBroadcaster : IPrinterBroadcaster
{
    private readonly IHubContext<PrinterHub> _hub;
    private readonly IPrinterRepository _repo;
    private readonly IMapper _mapper;

    public SignalRPrinterBroadcaster(
        IHubContext<PrinterHub> hub,
        IPrinterRepository repo,
        IMapper mapper)
    {
        _hub = hub;
        _repo = repo;
        _mapper = mapper;
    }

    public async Task BroadcastAllAsync(CancellationToken ct = default)
    {
        var printers = await _repo.GetAllAsync(ct);
        var dtos = _mapper.Map<IEnumerable<PrinterDto>>(printers);
        await _hub.Clients.All.SendAsync("ReceivePrinterUpdates", dtos, ct);
    }
}