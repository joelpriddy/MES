# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy solution & restore early (optional if you keep solution file at root)
COPY . .
RUN dotnet restore "src/Services/Manufacturing/PA.Manufacturing.Api/PA.Manufacturing.Api.csproj"

# publish
RUN dotnet publish "src/Services/Manufacturing/PA.Manufacturing.Api/PA.Manufacturing.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Health probe port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PA.Manufacturing.Api.dll"]
