using Microsoft.AspNetCore.SignalR;

namespace _3DPrinterSimulator.API.Hubs;

public class PrinterHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}