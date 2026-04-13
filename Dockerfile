# 1. Aşama: Derleme (Build)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Tüm dosyaları kopyala
COPY . .

# NuGet paketlerini geri yükle ve API projesini yayınla
# Yolu senin yapına göre (src/...) ayarladım
RUN dotnet publish src/3DPrinterSimulator.API/3DPrinterSimulator.API.csproj -c Release -o /out

# 2. Aşama: Çalıştırma (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .

# Railway için port ayarları
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Uygulamayı başlat (DLL adının doğruluğundan emin ol)
ENTRYPOINT ["dotnet", "3DPrinterSimulator.API.dll"]