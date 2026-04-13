using _3DPrinterSimulator.API.Hubs;
using _3DPrinterSimulator.API.Services;
using _3DPrinterSimulator.Application.Mapping;
using _3DPrinterSimulator.Data.Interfaces;
using _3DPrinterSimulator.Infrastructure;
using _3DPrinterSimulator.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CORS POLÝTÝKASI EKLE (Yeni eklendi) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://3d-printer-client.vercel.app") // Vite/React adresi
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR için zorunludur
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<PrinterProfile>());
builder.Services.AddAutoMapper(typeof(PrinterProfile));
builder.Services.AddSingleton<IPrinterBroadcaster, SignalRPrinterBroadcaster>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<IPrinterRepository>();
    await PrinterSeeder.SeedAsync(repo);
    app.Logger.LogInformation("10 yazýcý baþarýyla oluþturuldu.");
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseCors("ReactPolicy");

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<PrinterHub>("/hubs/printer");

app.Run();