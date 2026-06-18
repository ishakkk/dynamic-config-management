FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DynamicConfig.sln ./
COPY src/DynamicConfig.Domain/DynamicConfig.Domain.csproj src/DynamicConfig.Domain/
COPY src/DynamicConfig.Infrastructure/DynamicConfig.Infrastructure.csproj src/DynamicConfig.Infrastructure/
COPY src/DynamicConfig.Api/DynamicConfig.Api.csproj src/DynamicConfig.Api/

RUN dotnet restore src/DynamicConfig.Api/DynamicConfig.Api.csproj

COPY . .
RUN dotnet publish src/DynamicConfig.Api/DynamicConfig.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DynamicConfig.Api.dll"]
