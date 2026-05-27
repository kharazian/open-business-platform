FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OpenBusinessPlatform.Api.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM build AS migrator
WORKDIR /src
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.4
ENTRYPOINT ["/tools/dotnet-ef", "database", "update", "--project", "OpenBusinessPlatform.Api.csproj", "--startup-project", "OpenBusinessPlatform.Api.csproj"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
EXPOSE 5080

ENTRYPOINT ["dotnet", "OpenBusinessPlatform.Api.dll"]
