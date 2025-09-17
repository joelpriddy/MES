# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PA.MES.sln ./
COPY src/ ./src/
COPY tests/ ./tests/

WORKDIR /src/src/Services/Inventory/PA.Inventory.Api
RUN dotnet restore
RUN dotnet publish -c Release -o /app --no-restore

# ---- Run ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Defaults for compose (override locally as needed)
ENV HTTP_PORT=8080 \
    ConnectionStrings__Default="server=mysql;port=3306;database=pa_mes_inventory;user=root;password=root" \
    Kafka__BootstrapServers="redpanda:9092" \
    Kafka__GroupId="inventory-api"

EXPOSE 8080
ENTRYPOINT ["dotnet", "PA.Inventory.Api.dll"]
