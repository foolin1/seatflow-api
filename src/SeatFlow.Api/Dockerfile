FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["src/SeatFlow.Domain/SeatFlow.Domain.csproj", "src/SeatFlow.Domain/"]
COPY ["src/SeatFlow.Application/SeatFlow.Application.csproj", "src/SeatFlow.Application/"]
COPY ["src/SeatFlow.Infrastructure/SeatFlow.Infrastructure.csproj", "src/SeatFlow.Infrastructure/"]
COPY ["src/SeatFlow.Api/SeatFlow.Api.csproj", "src/SeatFlow.Api/"]

RUN dotnet restore \
    "src/SeatFlow.Api/SeatFlow.Api.csproj"

COPY . .

RUN dotnet publish \
    "src/SeatFlow.Api/SeatFlow.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SeatFlow.Api.dll"]