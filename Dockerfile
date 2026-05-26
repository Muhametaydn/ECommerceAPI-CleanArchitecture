# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sadece csproj dosyalarını kopyala → layer cache'ini verimli kullan
COPY ["src/ECommerce.API/ECommerce.API.csproj",                       "src/ECommerce.API/"]
COPY ["src/ECommerce.Application/ECommerce.Application.csproj",       "src/ECommerce.Application/"]
COPY ["src/ECommerce.Domain/ECommerce.Domain.csproj",                 "src/ECommerce.Domain/"]
COPY ["src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj", "src/ECommerce.Infrastructure/"]
COPY ["src/ECommerce.Persistence/ECommerce.Persistence.csproj",       "src/ECommerce.Persistence/"]

RUN dotnet restore "src/ECommerce.API/ECommerce.API.csproj"

# Tüm kaynak kodu kopyala ve publish et
COPY . .
WORKDIR "/src/src/ECommerce.API"
RUN dotnet publish "ECommerce.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root kullanıcı ile çalıştır (güvenlik)
RUN addgroup --system --gid 1001 appgroup \
 && adduser  --system --uid 1001 --ingroup appgroup appuser

COPY --from=build /app/publish .

# logs klasörü oluştur (Serilog file sink için)
RUN mkdir -p /app/logs && chown -R appuser:appgroup /app/logs

USER appuser

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS="http://+:8080"
ENV ASPNETCORE_ENVIRONMENT="Production"

ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
