FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base

WORKDIR /app

COPY . .
RUN dotnet restore

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 
WORKDIR /app
COPY --from=base /app/out .
ENTRYPOINT ["dotnet", "RinhaBack2024Q1.dll"]
