FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY MechanicsSoftware.ExecutionService.sln .
COPY src/MechanicsSoftware.ExecutionService.Api/MechanicsSoftware.ExecutionService.Api.csproj src/MechanicsSoftware.ExecutionService.Api/

RUN dotnet restore src/MechanicsSoftware.ExecutionService.Api/MechanicsSoftware.ExecutionService.Api.csproj

COPY src/ src/

RUN dotnet publish src/MechanicsSoftware.ExecutionService.Api/MechanicsSoftware.ExecutionService.Api.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MechanicsSoftware.ExecutionService.Api.dll"]
