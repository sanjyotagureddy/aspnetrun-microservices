#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ApiGateways/Shopping.Aggregator/Shopping.Aggregator.csproj", "ApiGateways/Shopping.Aggregator/"]
RUN dotnet restore "ApiGateways/Shopping.Aggregator/Shopping.Aggregator.csproj"
COPY . .
WORKDIR "/src/ApiGateways/Shopping.Aggregator"
RUN dotnet build "Shopping.Aggregator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Shopping.Aggregator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Shopping.Aggregator.dll"]
