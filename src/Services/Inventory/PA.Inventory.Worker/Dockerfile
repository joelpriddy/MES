# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# ---- copy solution and restore ----
COPY PA.MES.sln ./
COPY src/ ./src/
COPY tests/ ./tests/

# ---- Restore only the worker first to leverage layer caching ----
WORKDIR /src/src/Services/Inventory/PA.Inventory.Worker
RUN dotnet restore

# ---- Build (Release) ----
RUN dotnet publish -c Release -o /app --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Default ASP.NET Core container guidance uses non-root when possible
# but this is a console worker; root is fine unless you have policies.
# USER 64198

# Environment-driven config (override in docker-compose)
# These are just sensible defaults for running inside the same compose network.
ENV ConnectionStrings__Default="server=mysql;port=3306;database=pa_mes_inventory;user=root;password=root" \
    Kafka__BootstrapServers="redpanda:9092" \
    Kafka__GroupId="inventory-worker" \
    Kafka__TopicOrderCreated="orders.created" \
    Kafka__TopicOrderShipped="orders.shipped" \
    Kafka__TopicOrderCanceled="orders.canceled"

ENTRYPOINT ["dotnet", "PA.Inventory.Worker.dll"]
