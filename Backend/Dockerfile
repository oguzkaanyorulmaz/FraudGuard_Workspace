# 1. Aşama: Derleme (Build) Ortamı - Sürüm 10.0'a Yükseltildi!
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Backend klasöründeki her şeyi Docker'ın içine kopyala
COPY . ./

# API klasörüne gir (Loglardan klasör adının FraudGuard.API olduğu anlaşıldı)
WORKDIR /app/FraudGuard.API

# Projeyi .NET 10 ile restore et ve yayınla
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# 2. Aşama: Çalışma (Runtime) Ortamı - Sürüm 10.0'a Yükseltildi!
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Önceki aşamada oluşturulan derlenmiş dosyaları (/app/out içindekileri) al
COPY --from=build-env /app/out .

# ASP.NET Core'un dinleyeceği varsayılan port
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Çalıştırılacak ana DLL dosyası
ENTRYPOINT ["dotnet", "FraudGuard.API.dll"]