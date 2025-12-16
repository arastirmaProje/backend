# 1. Runtime (Çalışma) ortamı
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Render'ın dinlediği portu .NET'e bildiriyoruz (Kritik Satır) 👇
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# 2. Build (Derleme) ortamı
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore

# Derleme ve Yayınlama
# /p:UseAppHost=false ekledik, Linux'ta daha stabil çalışır
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# 3. Final (Canlı) ortamı
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Personelim.dll"]