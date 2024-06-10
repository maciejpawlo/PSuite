#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/APIGateway/PSuite.APIGateway/PSuite.APIGateway.csproj", "src/APIGateway/PSuite.APIGateway/"]
RUN dotnet restore "src/APIGateway/PSuite.APIGateway/PSuite.APIGateway.csproj"
COPY . .
WORKDIR "/src/src/APIGateway/PSuite.APIGateway"
RUN dotnet build "PSuite.APIGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PSuite.APIGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PSuite.APIGateway.dll"]
